using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.InitiateUserOnboarding.Contracts;

namespace Tooyioo.Tests.Features.InitiateUserOnboarding;

[Category(TestCategories.Conflict)]
public sealed class WhenUserLoginIsClaimedConcurrently
    : ConcurrentCommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;

    protected override Task Given()
        => Task.CompletedTask;

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject("google-sub-123")
            .Build();

        _response = await Host.HttpClient.PostJson(
            "/user-onboarding/initiate",
            new InitiateUserOnboardingRequest(),
            token);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_conflict()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);

    [Test]
    public async Task Then_response_title_is_concurrent_update()
        => await Assert.That(_body.Title).IsEqualTo("concurrent_update");
}
