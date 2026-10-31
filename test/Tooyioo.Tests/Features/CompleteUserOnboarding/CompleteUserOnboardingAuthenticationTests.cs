using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Auth;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.CompleteUserOnboarding.Contracts;

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
            new CompleteUserOnboardingRequest { TermsAndConditionsVersion = "v1" },
            authenticationScenario,
            host);

        await authenticationScenario.ShouldHaveExpectedProblemDetails(response);
    }
}
