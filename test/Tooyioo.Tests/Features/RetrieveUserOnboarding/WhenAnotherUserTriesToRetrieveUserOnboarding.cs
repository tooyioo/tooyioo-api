using System.Net;
using Slicent.Application;
using Tooyioo.Common;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.RetrieveUserOnboarding;

[Category(TestCategories.Authorization)]
public sealed class WhenAnotherUserTriesToRetrieveUserOnboarding
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserOnboardingId _ownerUserOnboardingId = null!;
    private string _anotherSubject = null!;

    protected override async Task Given()
    {
        _ownerUserOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        var ownerUserId = new UserId(2.ToGuid().ToString());
        _anotherSubject = "another-google-sub-456";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _ownerUserOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithUserId(ownerUserId)
                .Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId("owner-google-sub-123"),
            DomainEvent.UserLoginClaimed()
                .WithUserId(ownerUserId)
                .Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_anotherSubject),
            DomainEvent.UserLoginClaimed()
                .WithUserId(new UserId(3.ToGuid().ToString()))
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_anotherSubject)
            .Build();

        _response = await Host.HttpClient.Get(
            $"/user-onboarding/{_ownerUserOnboardingId.Value}",
            token);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_forbidden()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

    [Test]
    public async Task Then_response_title_is_forbidden()
        => await Assert.That(_body.Title).IsEqualTo("forbidden");
}
