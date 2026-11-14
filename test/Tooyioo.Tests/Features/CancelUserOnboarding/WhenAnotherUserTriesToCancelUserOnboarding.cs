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

[Category(TestCategories.Authorization)]
public sealed class WhenAnotherUserTriesToCancelUserOnboarding
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserId _ownerUserId = null!;
    private UserId _anotherUserId = null!;
    private string _ownerSubject = null!;
    private string _anotherSubject = null!;

    protected override async Task Given()
    {
        _ownerUserId = new UserId(1.ToGuid().ToString());
        _anotherUserId = new UserId(2.ToGuid().ToString());
        _ownerSubject = "owner-google-sub-123";
        _anotherSubject = "another-google-sub-456";

        await GivenUserOnboarding(_ownerUserId, _ownerSubject);
        await GivenUserOnboarding(_anotherUserId, _anotherSubject);
    }

    protected override async Task When()
    {
        var anotherUserToken = Host.GoogleIdentityToken()
            .WithSubject(_anotherSubject)
            .Build();

        _response = await Host.HttpClient.PutJson(
            $"/users/{_ownerUserId.Value}/onboarding/cancellation",
            new CancelUserOnboardingRequest { ReasonCode = "Changed mind" },
            anotherUserToken);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_forbidden()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

    [Test]
    public async Task Then_response_title_is_forbidden()
        => await Assert.That(_body.Title).IsEqualTo("forbidden");

    private async Task GivenUserOnboarding(UserId userOnboardingId, string subject)
    {
        await Host.Given<UserOnboardingState, UserId>(
            userOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(subject).Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(subject),
            DomainEvent.UserLoginClaimed().WithUserId(userOnboardingId).Build());
    }
}
