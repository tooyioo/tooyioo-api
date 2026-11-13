using Eventuous;
using Microsoft.Extensions.DependencyInjection;
using Slicent;
using static Tooyioo.Business.Contracts.BusinessOnboardingDomainEvents.V1;
using static Tooyioo.User.Contracts.UserLoginClaimingDomainEvents.V1;
using static Tooyioo.User.Contracts.UserPersonalDataDomainEvents.V1;
using static Tooyioo.User.Contracts.UserAliasClaimingDomainEvents.V1;
using static Tooyioo.User.Contracts.UserLoginLinkingDomainEvents.V1;
using static Tooyioo.User.Contracts.UserOnboardingDomainEvents.V1;

namespace Tooyioo.Tests.Support.Events;

[Category(TestCategories.Infrastructure)]
public sealed class WhenDomainEventsAreSerialized
{
    [Test]
    public async Task Then_user_onboarding_event_names_round_trips()
    {
        var services = new ServiceCollection();
        services.AddSlicent(
            typeof(User.Contracts.ContractsAssemblyMarker).Assembly,
            typeof(User.Contracts.ContractsAssemblyMarker).Assembly,
            typeof(User.Contracts.ContractsAssemblyMarker).Assembly);
        await using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<IEventSerializer>();

        (object Event, string Name)[] cases =
        [
            (new UserOnboardingInitiated("user-1"), "V1.UserOnboardingInitiated"),
            (new UserOnboardingAliasChosen("ada"), "V1.UserOnboardingAliasChosen"),
            (new UserOnboardingCompleted("v1", "v1"), "V1.UserOnboardingCompleted"),
            (new UserOnboardingCanceled("requested"), "V1.UserOnboardingCanceled"),
            (new UserAliasClaimed("user-1"), "V1.UserAliasClaimed"),
            (new UserAliasReleased("user-1"), "V1.UserAliasReleased"),
            (new UserLoginClaimed("user-1", "onboarding-1"), "V1.UserLoginClaimed"),
            (new UserLoginReleased("user-1"), "V1.UserLoginReleased"),
            (new UserLoginLinked("credential-key", "oidc", "https://accounts.google.com"), "V1.UserLoginLinked"),
            (new UserLoginUnlinked("credential-key"), "V1.UserLoginUnlinked"),
            (new UserPersonalDataRecorded("Ada", "Lovelace", "ada@example.com", true), "V1.UserPersonalDataRecorded"),
            (new UserPersonalDataErasureRequested("user_request", DateTime.UtcNow), "V1.UserPersonalDataErasureRequested"),
            (new UserPersonalDataErasureCanceled(), "V1.UserPersonalDataErasureCanceled"),
            (new UserPersonalDataErasureApplied(), "V1.UserPersonalDataErasureApplied")
        ];

        foreach (var (evt, name) in cases)
        {
            var stored = serializer.SerializeEvent(evt);
            var read = serializer.DeserializeEvent(stored.Payload, stored.EventType, stored.ContentType);

            await Assert.That(stored.EventType).IsEqualTo(name);
            await Assert.That(stored.ContentType).IsEqualTo("application/json");
            await Assert.That(((DeserializationResult.SuccessfullyDeserialized)read).Payload).IsEqualTo(evt);
        }
    }

    [Test]
    public async Task Then_business_onboarding_event_names_round_trip()
    {
        var services = new ServiceCollection();
        services.AddSlicent(typeof(Business.Contracts.ContractsAssemblyMarker).Assembly);
        await using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<IEventSerializer>();

        (object Event, string Name)[] cases =
        [
            (new BusinessOnboardingInitiated("Acme Ltd", "user-1"),
                "V1.BusinessOnboardingInitiated")
        ];

        foreach (var (evt, name) in cases)
        {
            var stored = serializer.SerializeEvent(evt);
            var read = serializer.DeserializeEvent(stored.Payload, stored.EventType, stored.ContentType);

            await Assert.That(stored.EventType).IsEqualTo(name);
            await Assert.That(stored.ContentType).IsEqualTo("application/json");
            await Assert.That(((DeserializationResult.SuccessfullyDeserialized)read).Payload).IsEqualTo(evt);
        }
    }

    [Test]
    public async Task Then_existing_web_json_is_readable()
    {
        var services = new ServiceCollection();
        services.AddSlicent(
            typeof(User.Contracts.ContractsAssemblyMarker).Assembly,
            typeof(User.Contracts.ContractsAssemblyMarker).Assembly,
            typeof(User.Contracts.ContractsAssemblyMarker).Assembly);
        await using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<IEventSerializer>();
        var payload = """
                      {"name":"Ada","lastName":"Lovelace","email":"ada@example.com","isEmailVerified":true}
                      """u8.ToArray();

        var read = serializer.DeserializeEvent(payload, "V1.UserPersonalDataRecorded", "application/json");

        await Assert.That(((DeserializationResult.SuccessfullyDeserialized)read).Payload)
            .IsEqualTo(new UserPersonalDataRecorded("Ada", "Lovelace", "ada@example.com", true));
    }
}
