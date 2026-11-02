using Tooyioo.Tests.Support.Extensions;
using System.Net;
using Tooyioo.Common;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Contracts;
using Tooyioo.User.Features.CompleteUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.CompleteUserOnboarding;

[Category(TestCategories.HappyPath)]
public sealed class WhenUserIdDiffersFromUserOnboardingId
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private CompleteUserOnboardingResponse _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private UserId _userId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _userId = new UserId(2.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithUserId(_userId.Value).Build(),
            DomainEvent.UserOnboardingAliasChosen().WithAlias("jane").Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserId(_userId)
                .WithUserOnboardingId(_userOnboardingId)
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/user-onboarding/{_userOnboardingId.Value}/complete",
            new CompleteUserOnboardingRequest
            {
                TermsOfServiceVersion = "v1",
                PrivacyPolicyVersion = "v1"
            },
            token);

        _body = await _response.ReadJson<CompleteUserOnboardingResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_the_person_user_id()
        => await Assert.That(_body.UserId).IsEqualTo(_userId.Value);

    [Test]
    public async Task Then_the_onboarding_stream_is_completed()
        => await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .ShouldContain<UserOnboardingDomainEvents.V1.UserOnboardingCompleted>();
}
