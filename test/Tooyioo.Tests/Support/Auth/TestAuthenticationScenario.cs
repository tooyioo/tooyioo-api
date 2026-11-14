using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Http;
// ReSharper disable ConvertToExtensionBlock

namespace Tooyioo.Tests.Support.Auth;

public enum TestAuthenticationScenario
{
    MissingToken,
    InvalidToken,
    MissingSubjectClaim,
    MissingIssuerClaim,
    UnsupportedIssuer,
    ExpiredToken
}

public static class TestAuthenticationScenarioExtensions
{
    private const string UnauthorizedProblemTitle = "unauthorized";
    private const string UnauthorizedProblemDetail = "Missing or invalid bearer token.";

    private static string? CreateBearerToken(
        this TestAuthenticationScenario scenario,
        VerticalSliceTestHost host)
        => scenario switch
        {
            TestAuthenticationScenario.MissingToken => null,
            TestAuthenticationScenario.InvalidToken => "not-a-valid-jwt",
            TestAuthenticationScenario.MissingSubjectClaim => host.GoogleIdentityToken().WithoutSubject().Build(),
            TestAuthenticationScenario.MissingIssuerClaim => host.GoogleIdentityToken().WithoutIssuer().Build(),
            TestAuthenticationScenario.UnsupportedIssuer => host.GoogleIdentityToken()
                .WithIssuer("https://unsupported.example.com")
                .Build(),
            TestAuthenticationScenario.ExpiredToken => host.GoogleIdentityToken().Expired().Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };

    private static HttpStatusCode ExpectedStatusCode(this TestAuthenticationScenario scenario)
        => scenario switch
        {
            TestAuthenticationScenario.MissingToken => HttpStatusCode.Unauthorized,
            TestAuthenticationScenario.InvalidToken => HttpStatusCode.Unauthorized,
            TestAuthenticationScenario.MissingSubjectClaim => HttpStatusCode.Unauthorized,
            TestAuthenticationScenario.MissingIssuerClaim => HttpStatusCode.Unauthorized,
            TestAuthenticationScenario.UnsupportedIssuer => HttpStatusCode.Unauthorized,
            TestAuthenticationScenario.ExpiredToken => HttpStatusCode.Unauthorized,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };

    public static Task<HttpResponseMessage> PostJson(
        this HttpClient client,
        string uri,
        object request,
        TestAuthenticationScenario scenario,
        VerticalSliceTestHost host)
        => client.PostJson(uri, request, scenario.CreateBearerToken(host));

    public static Task<HttpResponseMessage> PutJson(
        this HttpClient client,
        string uri,
        object request,
        TestAuthenticationScenario scenario,
        VerticalSliceTestHost host)
        => client.PutJson(uri, request, scenario.CreateBearerToken(host));

    public static Task<HttpResponseMessage> Get(
        this HttpClient client,
        string uri,
        TestAuthenticationScenario scenario,
        VerticalSliceTestHost host)
        => client.Get(uri, scenario.CreateBearerToken(host));

    public static async Task ShouldHaveExpectedProblemDetails(
        this TestAuthenticationScenario scenario,
        HttpResponseMessage response)
    {
        var body = await response.ReadJson<HttpProblemDetails>();

        await Assert.That(response.StatusCode).IsEqualTo(scenario.ExpectedStatusCode());
        await Assert.That(body.Title).IsEqualTo(UnauthorizedProblemTitle);
        await Assert.That(body.Detail).IsEqualTo(UnauthorizedProblemDetail);
    }
}
