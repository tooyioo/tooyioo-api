using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.CancelUserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.CancelUserOnboarding;

public sealed class WhenAnotherUserTriesToCancelUserOnboarding
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserOnboardingId _ownerUserOnboardingId = null!;
    private UserOnboardingId _anotherUserOnboardingId = null!;
    private string _ownerSubject = null!;
    private string _anotherSubject = null!;

    protected override async Task Given()
    {
        _ownerUserOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _anotherUserOnboardingId = new UserOnboardingId(2.ToGuid().ToString());
        _ownerSubject = "owner-google-sub-123";
        _anotherSubject = "another-google-sub-456";

        await GivenUserOnboarding(_ownerUserOnboardingId, _ownerSubject);
        await GivenUserOnboarding(_anotherUserOnboardingId, _anotherSubject);
    }

    protected override async Task When()
    {
        var anotherUserToken = Host.GoogleIdentityToken()
            .WithSubject(_anotherSubject)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/user-onboarding/{_ownerUserOnboardingId.Value}/cancel",
            new CancelUserOnboardingRequest { Reason = "Changed mind" },
            anotherUserToken);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_forbidden()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

    [Test]
    public async Task Then_response_title_is_forbidden()
        => await Assert.That(_body.Title).IsEqualTo("forbidden");

    private async Task GivenUserOnboarding(UserOnboardingId userOnboardingId, string subject)
    {
        await Host.Given<UserOnboardingState, UserOnboardingId>(
            userOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(subject).Build());

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(subject),
            DomainEvent.ExternalIdentityClaimed().WithUserOnboardingId(userOnboardingId).Build());
    }
}
