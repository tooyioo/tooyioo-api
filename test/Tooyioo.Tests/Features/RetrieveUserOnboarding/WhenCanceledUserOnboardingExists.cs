using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.RetrieveUserOnboarding;

[Category(TestCategories.Authorization)]
public sealed class WhenCanceledUserOnboardingExists
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed().WithUserOnboardingId(_userOnboardingId).Build(),
            DomainEvent.UserLoginReleased().WithUserOnboardingId(_userOnboardingId).Build());

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(_subject).Build(),
            DomainEvent.UserOnboardingCanceled().Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.Get($"/user-onboarding/{_userOnboardingId.Value}", token);
        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_forbidden()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

    [Test]
    public async Task Then_response_title_is_forbidden()
        => await Assert.That(_body.Title).IsEqualTo("forbidden");
}
