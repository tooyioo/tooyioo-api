using Tooyioo.User.Contracts;
using Eventuous;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Context;
using Eventuous.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.User;
using Tooyioo.User.Features.Support;

// ReSharper disable MemberCanBePrivate.Global

namespace Tooyioo.Tests.Support;

public sealed class VerticalSliceTestHost
    : IAsyncDisposable
{
    private readonly VerticalSliceWebApplicationFactory _factory;
    private readonly byte[] _signingKey;
    private readonly MongoClient? _mongoClient;
    private readonly string? _mongoDatabaseName;

    private VerticalSliceTestHost(
        VerticalSliceWebApplicationFactory factory,
        HttpClient httpClient,
        InMemoryEventStore eventStore,
        byte[] signingKey,
        MongoClient? mongoClient,
        string? mongoDatabaseName)
    {
        _factory = factory;
        _signingKey = signingKey;
        _mongoClient = mongoClient;
        _mongoDatabaseName = mongoDatabaseName;
        HttpClient = httpClient;
        EventStore = eventStore;
        Events = new EventAssertions(eventStore);
    }

    public HttpClient HttpClient { get; }
    public InMemoryEventStore EventStore { get; }
    public EventAssertions Events { get; }
    internal IServiceProvider Services => _factory.Services;

    public static Task<VerticalSliceTestHost> Start(Action<IServiceCollection>? overrideServices = null)
        => Start(null, overrideServices);

    public static Task<VerticalSliceTestHost> Start(
        MongoReadModelTestDatabase? mongoDatabase,
        Action<IServiceCollection>? overrideServices = null)
    {
        var eventStore = new InMemoryEventStore();
        var signingKey = GoogleIdentityTokenBuilder.CreateSigningKey();
        var mongoOptions = mongoDatabase is null
            ? null
            : new MongoDatabaseTestOptions(mongoDatabase.ConnectionString, mongoDatabase.DatabaseName);
        var factory = new VerticalSliceWebApplicationFactory(eventStore, signingKey, mongoOptions, overrideServices);
        var client = factory.CreateClient();
        var mongoClient = mongoDatabase is null
            ? null
            : new MongoClient(mongoDatabase.ConnectionString);

        return Task.FromResult(new VerticalSliceTestHost(
            factory,
            client,
            eventStore,
            signingKey,
            mongoClient,
            mongoDatabase?.DatabaseName));
    }

    public GoogleIdentityTokenBuilder GoogleIdentityToken()
        => new(_signingKey);

    public async Task Given<TState, TId>(TId id, params object[] events)
        where TState : State<TState, TId>, new()
        where TId : Id
    {
        events = NormalizeEvents(id, events);
        var stream = await EventStore.LoadStateOrNew<TState, TId>(id, CancellationToken.None);
        await EventStore.StoreStateChanges([stream.ToStateStreamChanges(events)], CancellationToken.None);
    }

    public Task GivenAndProject<TProjector, TState, TId>(TId id, params object[] events)
        where TProjector : IEventHandler
        where TState : State<TState, TId>, new()
        where TId : Id
        => GivenAndProject<TProjector, TState, TId>(id, DateTime.UtcNow, events);

    public async Task GivenAndProject<TProjector, TState, TId>(
        TId id,
        DateTime createdUtc,
        params object[] events)
        where TProjector : IEventHandler
        where TState : State<TState, TId>, new()
        where TId : Id
    {
        events = NormalizeEvents(id, events);
        var stream = await EventStore.LoadStateOrNew<TState, TId>(id, CancellationToken.None);
        var changes = stream.ToStateStreamChanges(events);
        await EventStore.StoreStateChanges([changes], CancellationToken.None);

        using var scope = _factory.Services.CreateScope();
        var projector = ActivatorUtilities.CreateInstance<TProjector>(scope.ServiceProvider);
        var startingEventNumber = (ulong)stream.Events.Length;

        for (var index = 0; index < events.Length; index++)
        {
            var evt = events[index];
            var position = startingEventNumber + (ulong)index;
            var context = new MessageConsumeContext(
                Guid.NewGuid().ToString("N"),
                evt.GetType().Name,
                "application/json",
                changes.StreamName.ToString(),
                position,
                position,
                position,
                position,
                DateTime.SpecifyKind(createdUtc, DateTimeKind.Utc),
                evt,
                new Metadata(),
                "ReadModelTests",
                CancellationToken.None);

            _ = await projector.HandleEvent(context);
        }
    }

    private static object[] NormalizeEvents<TId>(TId id, object[] events)
        where TId : Id
    {
        if (id is not UserOnboardingId onboardingId)
        {
            return events;
        }

        return
        [
            .. events
                .Select(evt => evt is UserOnboardingDomainEvents.V1.UserOnboardingInitiated initiated
                               && string.IsNullOrWhiteSpace(initiated.UserId)
                    ? new UserOnboardingDomainEvents.V1.UserOnboardingInitiated(onboardingId.Value)
                    : evt)
        ];
    }

    public Task GivenUserPersonalData(
        UserOnboardingId userId,
        string name = "Jane",
        string lastName = "Bloggs",
        string email = "jane.bloggs@test.com",
        bool isEmailVerified = false)
        => Given<UserPersonalDataState, UserPersonalDataId>(
            new UserPersonalDataId(userId.Value),
            DomainEvent.UserPersonalDataRecorded()
                .WithName(name)
                .WithLastName(lastName)
                .WithEmail(email)
                .WithIsEmailVerified(isEmailVerified)
                .Build());

    public async Task<TResult> SendCommand<TResult>(ICommand<TResult> command)
    {
        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        return await dispatcher.Send(command, CancellationToken.None);
    }

    public Task GivenLinkedLogin(UserOnboardingId userId, string subject)
        => Given<UserLoginLinkingState, UserLoginLinkingId>(
            new UserLoginLinkingId(userId.Value),
            DomainEvent.UserLoginLinked()
                .WithUserLoginKey(subject)
                .Build());

    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        await _factory.DisposeAsync();
        if (_mongoClient is not null && !string.IsNullOrWhiteSpace(_mongoDatabaseName))
        {
            await _mongoClient.DropDatabaseAsync(_mongoDatabaseName);
        }
    }
}
