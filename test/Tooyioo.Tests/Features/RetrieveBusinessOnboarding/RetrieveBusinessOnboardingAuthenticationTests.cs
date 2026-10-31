using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Auth;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;

namespace Tooyioo.Tests.Features.RetrieveBusinessOnboarding;

[Category(TestCategories.Authentication)]
[Category(TestCategories.Authorization)]
public sealed class RetrieveBusinessOnboardingAuthenticationTests
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

        var businessOnboardingId = 1.ToGuid();
        using var response = await host.HttpClient.Get(
            $"/business-onboarding/{businessOnboardingId}",
            authenticationScenario,
            host);

        await authenticationScenario.ShouldHaveExpectedProblemDetails(response);
    }

    [Test]
    public async Task Endpoint_rejects_users_who_have_not_completed_user_onboarding()
    {
        await using var host = await VerticalSliceTestHost.Start();
        var businessOnboardingId = 1.ToGuid();
        var token = host.GoogleIdentityToken()
            .WithSubject("google-sub-123")
            .Build();

        using var response = await host.HttpClient.Get(
            $"/business-onboarding/{businessOnboardingId}",
            token);

        var body = await response.ReadJson<HttpProblemDetails>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(body.Title).IsEqualTo("forbidden");
    }
}
