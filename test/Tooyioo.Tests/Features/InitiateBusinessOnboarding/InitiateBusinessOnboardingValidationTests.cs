using System.Net;
using Slicent.Application;
using Tooyioo.Business.Features.InitiateBusinessOnboarding.Contracts;
using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.InitiateBusinessOnboarding;

[Category(TestCategories.Validation)]
public sealed class InitiateBusinessOnboardingValidationTests
{
    [Test]
    [Arguments("not-provided", null, "legal_name_required")]
    [Arguments("empty", 0, "legal_name_required")]
    [Arguments("too-long", InitiateBusinessOnboardingRequest.LegalNameMaxLength + 1, "legal_name_too_long")]
    public async Task Endpoint_rejects_invalid_payloads(
        string _,
        int? legalNameLength,
        params string[] expectedErrors)
    {
        await using var host = await VerticalSliceTestHost.Start();

        const string subject = "google-sub-123";
        var userOnboardingId = new UserId(1.ToGuid().ToString());
        var userId = 2.ToGuid().ToString();

        await host.Given<UserOnboardingState, UserId>(
            userOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(subject).Build(),
            DomainEvent.UserOnboardingCompleted().WithUserId(userId).Build());

        await host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(subject),
            DomainEvent.UserLoginClaimed().WithUserId(userOnboardingId).Build());

        var token = host.GoogleIdentityToken()
            .WithSubject(subject)
            .Build();

        string? legalName = legalNameLength switch
        {
            null => null,
            0 => string.Empty,
            _ => new string('a', legalNameLength.Value)
        };

        object request = legalName is null
            ? new { }
            : new InitiateBusinessOnboardingRequest { LegalName = legalName };

        using var response = await host.HttpClient.PostJson(
            "/businesses",
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
