using System.Net;
using Slicent.Application;
using Tooyioo.BusinessOnboarding.Features.InitiateBusinessOnboarding.Contracts;
using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Auth;
using Tooyioo.Tests.Support.Http;

namespace Tooyioo.Tests.Features.InitiateBusinessOnboarding;

public sealed class InitiateBusinessOnboardingAuthenticationTests
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

        using var response = await host.HttpClient.PostJson(
            "/business-onboarding/initiate",
            new InitiateBusinessOnboardingRequest { LegalName = "Acme Ltd" },
            authenticationScenario,
            host);

        await authenticationScenario.ShouldHaveExpectedProblemDetails(response);
    }

    [Test]
    public async Task Endpoint_rejects_authenticated_users_who_have_not_completed_user_onboarding()
    {
        await using var host = await VerticalSliceTestHost.Start();
        var token = host.GoogleIdentityToken()
            .WithSubject("google-sub-123")
            .Build();

        using var response = await host.HttpClient.PostJson(
            "/business-onboarding/initiate",
            new InitiateBusinessOnboardingRequest { LegalName = "Acme Ltd" },
            token);

        var body = await response.ReadJson<HttpProblemDetails>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(body.Title).IsEqualTo("forbidden");
    }
}
