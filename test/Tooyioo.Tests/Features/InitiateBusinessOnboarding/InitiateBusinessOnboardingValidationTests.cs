using System.Net;
using Slicent.Application;
using Tooyioo.BusinessOnboarding.Features.InitiateBusinessOnboarding.Contracts;
using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.InitiateBusinessOnboarding;

public sealed class InitiateBusinessOnboardingValidationTests
{
    [Test]
    [Arguments("not-provided", null, "legal_name_required")]
    [Arguments("empty", "", "legal_name_required")]
    public async Task Endpoint_rejects_invalid_payloads(
        string _,
        string? legalName,
        params string[] expectedErrors)
    {
        await using var host = await VerticalSliceTestHost.Start();

        const string subject = "google-sub-123";
        var userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        var userId = 2.ToGuid().ToString();

        await host.Given<UserOnboardingState, UserOnboardingId>(
            userOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(subject).Build(),
            DomainEvent.UserOnboardingCompleted().WithUserId(userId).Build());

        await host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(subject),
            DomainEvent.ExternalIdentityClaimed().WithUserOnboardingId(userOnboardingId).Build());

        var token = host.GoogleIdentityToken()
            .WithSubject(subject)
            .Build();

        object request = legalName is null
            ? new { }
            : new InitiateBusinessOnboardingRequest { LegalName = legalName };

        using var response = await host.HttpClient.PostJson(
            "/business-onboarding/initiate",
            request,
            token);

        var body = await response.ReadJson<HttpProblemDetails>();
        var errorsForLegalName =
            body.Errors is not null && body.Errors.TryGetValue("legalName", out var errors)
                ? errors
                : [];

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(body.Title).IsEqualTo("validation_failed");

        foreach (var expectedError in expectedErrors)
        {
            await Assert.That(errorsForLegalName.Contains(expectedError)).IsTrue();
        }
    }
}
