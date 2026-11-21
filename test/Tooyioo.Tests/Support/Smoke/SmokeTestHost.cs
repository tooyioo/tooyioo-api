using System.Net;
using DotNet.Testcontainers.Builders;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.User.Features.RetrieveUser.Contract;
using TUnit.Core.Interfaces;

namespace Tooyioo.Tests.Support.Smoke;

/// <summary>
/// Shared smoke-test host: real KurrentDB and MongoDB containers plus the API running
/// with its production event store, projections and hosted services.
/// </summary>
public sealed class SmokeTestHost
    : IAsyncInitializer, IAsyncDisposable
{
    private const string TestPepper = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private readonly byte[] _signingKey = GoogleIdentityTokenBuilder.CreateSigningKey();

    private MongoTestContainer? _mongo;
    private KurrentDbTestContainer? _kurrent;
    private bool _kurrentStarted;
    private SmokeWebApplicationFactory? _factory;

    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _mongo = new MongoTestContainer();
        var database = await _mongo.CreateDatabase();

        _kurrent = new KurrentDbTestContainer();

        try
        {
            await _kurrent.StartAsync();
            _kurrentStarted = true;
        }
        catch (DockerUnavailableException)
        {
            Skip.Test("Podman (or Docker) is required for smoke tests");
            throw;
        }

        _factory = new SmokeWebApplicationFactory(_signingKey);

        // appsettings.json wins over host configuration for existing keys. Environment
        // variables are read by WebApplication.CreateBuilder after appsettings, so they are
        // the only override that reaches the connection strings before AddSlicent reads them.
        // They are restored once the host has started; the registered clients keep the values.
        var previousKurrent = Environment.GetEnvironmentVariable("ConnectionStrings__KurrentDb");
        var previousMongo = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb");
        var previousPepper = Environment.GetEnvironmentVariable("UserLogin__Pepper");

        Environment.SetEnvironmentVariable("ConnectionStrings__KurrentDb", _kurrent.ConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__MongoDb", database.ConnectionString);
        Environment.SetEnvironmentVariable("UserLogin__Pepper", TestPepper);

        try
        {
            HttpClient = _factory.CreateClient();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__KurrentDb", previousKurrent);
            Environment.SetEnvironmentVariable("ConnectionStrings__MongoDb", previousMongo);
            Environment.SetEnvironmentVariable("UserLogin__Pepper", previousPepper);
        }
    }

    public string Token(string subject, string name, string lastName, string email)
        => new GoogleIdentityTokenBuilder(_signingKey)
            .WithSubject(subject)
            .WithPersonalDetails(name, lastName, email, true)
            .Build();

    /// <summary>
    /// Polls the read endpoint until the projection reflects the expected state.
    /// Projections are asynchronous, so a command returning successfully is not enough.
    /// </summary>
    public async Task<RetrieveUserResponse> WaitForUserAsync(
        string userId,
        string token,
        Func<RetrieveUserResponse, bool> predicate,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        string? lastStatus = null;

        while (DateTime.UtcNow < deadline)
        {
            using var response = await HttpClient.Get($"/users/{userId}", token);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var user = await response.ReadJson<RetrieveUserResponse>();

                if (predicate(user))
                {
                    return user;
                }

                lastStatus = user.Status;
            }
            else if (response.StatusCode != HttpStatusCode.NotFound)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Unexpected {response.StatusCode} while waiting for user {userId}: {body}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        throw new TimeoutException(
            $"User {userId} was not projected within the timeout (last status: {lastStatus ?? "not found"}).");
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_kurrent is not null && _kurrentStarted)
        {
            await _kurrent.DisposeAsync();
        }

        if (_mongo is not null)
        {
            await _mongo.DisposeAsync();
        }
    }
}
