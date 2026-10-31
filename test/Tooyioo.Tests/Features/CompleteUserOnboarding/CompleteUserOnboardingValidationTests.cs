using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.CompleteUserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.CompleteUserOnboarding;

public sealed class CompleteUserOnboardingValidationTests
{
    [Test]
    [Arguments("not-provided", null, "terms_and_conditions_version_required")]
    [Arguments("empty", 0, "terms_and_conditions_version_required")]
    [Arguments("too-long", CompleteUserOnboardingRequest.TermsAndConditionsVersionMaxLength + 1, "terms_and_conditions_version_too_long")]
    public async Task Endpoint_rejects_invalid_payloads(
        string _,
        int? termsAndConditionsVersionLength,
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

        string? termsAndConditionsVersion = termsAndConditionsVersionLength switch
        {
            null => null,
            0 => string.Empty,
            _ => new string('v', termsAndConditionsVersionLength.Value)
        };

        object request = termsAndConditionsVersion is null
            ? new { }
            : new CompleteUserOnboardingRequest { TermsAndConditionsVersion = termsAndConditionsVersion };

        using var response = await host.HttpClient.PostJson(
            $"/user-onboarding/{userOnboardingId.Value}/complete",
            request,
            token);

        var body = await response.ReadJson<HttpProblemDetails>();
        var errorsForTermsAndConditionsVersion =
            body.Errors is not null && body.Errors.TryGetValue("termsAndConditionsVersion", out var errors)
                ? errors
                : [];

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(body.Title).IsEqualTo("validation_failed");

        foreach (var expectedError in expectedErrors)
        {
            await Assert.That(errorsForTermsAndConditionsVersion.Contains(expectedError)).IsTrue();
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

        await host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(subject),
            DomainEvent.ExternalIdentityClaimed()
                .WithUserOnboardingId(userOnboardingId)
                .Build());
    }
}
