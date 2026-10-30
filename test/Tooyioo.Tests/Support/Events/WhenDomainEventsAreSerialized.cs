using Eventuous;
using Microsoft.Extensions.DependencyInjection;
using Slicent;
using Tooyioo.UserOnboarding.Contracts;
using static Tooyioo.UserOnboarding.Contracts.UserAliasClaimingDomainEvents.V1;
using static Tooyioo.UserOnboarding.Contracts.ExternalIdentityClaimingDomainEvents.V1;
using static Tooyioo.UserOnboarding.Contracts.UserOnboardingDomainEvents.V1;

namespace Tooyioo.Tests.Support.Events;

public sealed class WhenDomainEventsAreSerialized
{
    [Test]
    public async Task Then_every_stored_event_name_round_trips()
    {
        var services = new ServiceCollection();
        services.AddSlicent(typeof(ContractsAssemblyMarker).Assembly);
        await using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<IEventSerializer>();

        (object Event, string Name)[] cases =
        [
            (new UserOnboardingInitiated("Ada", "Lovelace", "ada@example.com", 
                true, "google-1", "Google", "issuer"), 
                "V1.UserOnboardingInitiated"),
            (new UserOnboardingAliasChosen("ada"), "V1.UserOnboardingAliasChosen"),
            (new UserOnboardingCompleted("user-1", "v1"), "V1.UserOnboardingCompleted"),
            (new UserOnboardingCanceled("requested"), "V1.UserOnboardingCanceled"),
            (new UserAliasClaimed("onboarding-1"), "V1.AliasClaimed"),
            (new UserAliasReleased("onboarding-1"), "V1.AliasReleased"),
            (new ExternalIdentityClaimed("onboarding-1"), "V1.ExternalIdentityClaimed"),
            (new ExternalIdentityReleased("onboarding-1"), "V1.ExternalIdentityReleased")
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
        services.AddSlicent(typeof(ContractsAssemblyMarker).Assembly);
        await using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<IEventSerializer>();
        var payload = """
                      {"name":"Ada","lastName":"Lovelace","email":"ada@example.com",
                      "isEmailVerified":true,"externalIdentityId":"google-1",
                      "externalIdentityProvider":"Google","externalIdentityIssuer":"issuer"}
                      """u8.ToArray();

        var read = serializer.DeserializeEvent(payload, "V1.UserOnboardingInitiated", "application/json");

        await Assert.That(((DeserializationResult.SuccessfullyDeserialized)read).Payload)
            .IsEqualTo(new UserOnboardingInitiated(
                "Ada", 
                "Lovelace", 
                "ada@example.com", 
                true, 
                "google-1", 
                "Google", 
                "issuer"));
    }
}
