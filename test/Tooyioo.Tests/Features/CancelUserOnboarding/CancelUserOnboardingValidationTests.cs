using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.User.Features.CancelUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.CancelUserOnboarding;

[Category(TestCategories.Validation)]
public sealed class CancelUserOnboardingValidationTests
{
    [Test]
    [Arguments("not-provided", null, "reason_code_required")]
    [Arguments("empty", 0, "reason_code_required")]
    [Arguments("too-long", CancelUserOnboardingRequest.ReasonCodeMaxLength + 1, "reason_code_too_long")]
    public async Task Endpoint_rejects_invalid_payloads(
        string _,
        int? reasonLength,
        params string[] expectedErrors)
    {
        await using var host = await VerticalSliceTestHost.Start();

        const string subject = "google-sub-123";
        var userOnboardingId = new UserId(1.ToGuid().ToString());

        await host.Given<UserOnboardingState, UserId>(
            userOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(subject).Build());

        await host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(subject),
            DomainEvent.UserLoginClaimed().WithUserId(userOnboardingId).Build());

        var userOnboardingStreamBeforeWhen = await host.Events
            .Stream<UserOnboardingState, UserId>(userOnboardingId)
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
            : new CancelUserOnboardingRequest { ReasonCode = reason };

        using var response = await host.HttpClient.PutJson(
            $"/users/{userOnboardingId.Value}/onboarding/cancellation",
            request,
            token);

        var body = await response.ReadJson<HttpProblemDetails>();
        var errorsForReason =
            body.Errors is not null && body.Errors.TryGetValue("reasonCode", out var errors)
                ? errors
                : [];

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(body.Title).IsEqualTo("validation_failed");

        foreach (var expectedError in expectedErrors)
        {
            await Assert.That(errorsForReason.Contains(expectedError)).IsTrue();
        }

        await host.Events
            .Stream<UserOnboardingState, UserId>(userOnboardingId)
            .ShouldHaveNoChangesSince(userOnboardingStreamBeforeWhen);
    }
}
