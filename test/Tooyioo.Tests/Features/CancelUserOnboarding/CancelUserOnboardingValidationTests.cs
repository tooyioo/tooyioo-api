using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.CancelUserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.CancelUserOnboarding;

[Category(TestCategories.Validation)]
public sealed class CancelUserOnboardingValidationTests
{
    [Test]
    [Arguments("not-provided", null, "reason_required")]
    [Arguments("empty", 0, "reason_required")]
    [Arguments("too-long", CancelUserOnboardingRequest.ReasonMaxLength + 1, "reason_too_long")]
    public async Task Endpoint_rejects_invalid_payloads(
        string _,
        int? reasonLength,
        params string[] expectedErrors)
    {
        await using var host = await VerticalSliceTestHost.Start();

        const string subject = "google-sub-123";
        var userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());

        await host.Given<UserOnboardingState, UserOnboardingId>(
            userOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(subject).Build());

        await host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(subject),
            DomainEvent.ExternalIdentityClaimed().WithUserOnboardingId(userOnboardingId).Build());

        var userOnboardingStreamBeforeWhen = await host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(userOnboardingId)
            .CaptureSnapshot();

        var token = host.GoogleIdentityToken()
            .WithSubject(subject)
            .Build();

        string? reason = reasonLength switch
        {
            null => null,
            0 => string.Empty,
            _ => new string('a', reasonLength.Value)
        };

        object request = reason is null
            ? new { }
            : new CancelUserOnboardingRequest { Reason = reason };

        using var response = await host.HttpClient.PostJson(
            $"/user-onboarding/{userOnboardingId.Value}/cancel",
            request,
            token);

        var body = await response.ReadJson<HttpProblemDetails>();
        var errorsForReason =
            body.Errors is not null && body.Errors.TryGetValue("reason", out var errors)
                ? errors
                : [];

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(body.Title).IsEqualTo("validation_failed");

        foreach (var expectedError in expectedErrors)
        {
            await Assert.That(errorsForReason.Contains(expectedError)).IsTrue();
        }

        await host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(userOnboardingId)
            .ShouldHaveNoChangesSince(userOnboardingStreamBeforeWhen);
    }
}
