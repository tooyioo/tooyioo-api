using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Auth;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.User;
using Tooyioo.User.Features.CompleteUserOnboarding.Contracts;

namespace Tooyioo.Tests.Features.CompleteUserOnboarding;

[Category(TestCategories.Authentication)]
public sealed class CompleteUserOnboardingAuthenticationTests
{
    [Test]
    [Arguments(TestAuthenticationScenario.MissingToken)]
    [Arguments(TestAuthenticationScenario.InvalidToken)]
    [Arguments(TestAuthenticationScenario.MissingSubjectClaim)]
    [Arguments(TestAuthenticationScenario.MissingIssuerClaim)]
    [Arguments(TestAuthenticationScenario.UnsupportedIssuer)]
    [Arguments(TestAuthenticationScenario.ExpiredToken)]
    public async Task Endpoint_rejects_unauthenticated_requests(
        TestAuthenticationScenario authenticationScenario)
    {
        await using var host = await VerticalSliceTestHost.Start();

        var userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        using var response = await host.HttpClient.PostJson(
            $"/user-onboarding/{userOnboardingId.Value}/complete",
            new CompleteUserOnboardingRequest
            {
                TermsOfServiceVersion = "v1",
                PrivacyPolicyVersion = "v1"
            },
            authenticationScenario,
            host);

        await authenticationScenario.ShouldHaveExpectedProblemDetails(response);
    }
    [Test]
    [Category(TestCategories.Authorization)]
    public async Task Endpoint_rejects_authenticated_users_without_user_id()
    {
        await using var host = await VerticalSliceTestHost.Start();
        var userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        var token = host.GoogleIdentityToken()
            .WithSubject("google-sub-123")
            .Build();

        using var response = await host.HttpClient.PostJson(
            $"/user-onboarding/{userOnboardingId.Value}/complete",
            new CompleteUserOnboardingRequest { TermsOfServiceVersion = "v1", PrivacyPolicyVersion = "v1" },
            token);

        var body = await response.ReadJson<HttpProblemDetails>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(body.Title).IsEqualTo("forbidden");
    }

}
