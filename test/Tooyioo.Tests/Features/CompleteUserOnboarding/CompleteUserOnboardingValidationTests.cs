using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.User;
using Tooyioo.User.Features.CompleteUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.CompleteUserOnboarding;

[Category(TestCategories.Validation)]
public sealed class CompleteUserOnboardingValidationTests
{
    [Test]
    [Arguments("terms-not-provided", null, 2, "termsOfServiceVersion", "terms_of_service_version_required")]
    [Arguments("terms-empty", 0, 2, "termsOfServiceVersion", "terms_of_service_version_required")]
    [Arguments("terms-too-long", CompleteUserOnboardingRequest.VersionMaxLength + 1, 2, "termsOfServiceVersion", "terms_of_service_version_too_long")]
    [Arguments("privacy-not-provided", 2, null, "privacyPolicyVersion", "privacy_policy_version_required")]
    [Arguments("privacy-empty", 2, 0, "privacyPolicyVersion", "privacy_policy_version_required")]
    [Arguments("privacy-too-long", 2, CompleteUserOnboardingRequest.VersionMaxLength + 1, "privacyPolicyVersion", "privacy_policy_version_too_long")]
    public async Task Endpoint_rejects_invalid_payloads(
        string _,
        int? termsOfServiceVersionLength,
        int? privacyPolicyVersionLength,
        string field,
        params string[] expectedErrors)
    {
        await using var host = await VerticalSliceTestHost.Start();

        const string subject = "google-sub-123";
        var userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());

        await GivenUserOnboarding(host, userOnboardingId, subject);

        var userOnboardingStreamBeforeWhen = await host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(userOnboardingId)
            .CaptureSnapshot();

        var token = host.GoogleIdentityToken()
            .WithSubject(subject)
            .Build();

        var payload = new Dictionary<string, object?>();

        if (termsOfServiceVersionLength is not null)
        {
            payload["termsOfServiceVersion"] = termsOfServiceVersionLength switch
            {
                0 => string.Empty,
                _ => new string('v', termsOfServiceVersionLength.Value)
            };
        }

        if (privacyPolicyVersionLength is not null)
        {
            payload["privacyPolicyVersion"] = privacyPolicyVersionLength switch
            {
                0 => string.Empty,
                _ => new string('v', privacyPolicyVersionLength.Value)
            };
        }

        using var response = await host.HttpClient.PostJson(
            $"/user-onboarding/{userOnboardingId.Value}/complete",
            payload,
            token);

        var body = await response.ReadJson<HttpProblemDetails>();
        var errorsForField =
            body.Errors is not null && body.Errors.TryGetValue(field, out var errors)
                ? errors
                : [];

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(body.Title).IsEqualTo("validation_failed");

        foreach (var expectedError in expectedErrors)
        {
            await Assert.That(errorsForField.Contains(expectedError)).IsTrue();
        }

        await host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(userOnboardingId)
            .ShouldHaveNoChangesSince(userOnboardingStreamBeforeWhen);
    }

    private static async Task GivenUserOnboarding(
        VerticalSliceTestHost host,
        UserOnboardingId userOnboardingId,
        string subject)
    {
        await host.Given<UserOnboardingState, UserOnboardingId>(
            userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithName("Jane")
                .WithLastName("Bloggs")
                .WithEmail("jane.bloggs@test.com")
                .WithExternalIdentityId(subject)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("jane-bloggs")
                .Build());

        await host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(subject),
            DomainEvent.UserLoginClaimed()
                .WithUserOnboardingId(userOnboardingId)
                .Build());
    }
}
