using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.CancelUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.CancelUserOnboarding;

[Category(TestCategories.Conflict)]
public sealed class WhenUserOnboardingDoesNotExist
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserId _userOnboardingId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserId(1.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserId(_userOnboardingId)
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PutJson(
            $"/users/{_userOnboardingId.Value}/onboarding/cancellation",
            new CancelUserOnboardingRequest { ReasonCode = "changed_mind" },
            token);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_conflict()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

    [Test]
    public async Task Then_response_title_is_user_onboarding_not_found()
        => await Assert.That(_body.Title).IsEqualTo("user_onboarding_not_found");
}
