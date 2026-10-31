using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Auth;

namespace Tooyioo.Tests.Features.RetrieveUsers;

[Category(TestCategories.Authentication)]
public sealed class RetrieveUsersAuthenticationTests
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

        using var response = await host.HttpClient.Get(
            "/users",
            authenticationScenario,
            host);

        await authenticationScenario.ShouldHaveExpectedProblemDetails(response);
    }
}
