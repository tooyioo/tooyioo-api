using System.Net;
using Tooyioo.Common;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.InitiateUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.InitiateUserOnboarding;

[Category(TestCategories.HappyPath)]
public sealed class WhenGoogleIdentityWasReleased
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private InitiateUserOnboardingResponse _body = null!;
    private UserOnboardingId _canceledUserOnboardingId = null!;
    private string _subject = null!;
    private string _name = null!;
    private string _lastName = null!;
    private string _email = null!;
    private EventStreamSnapshot _externalIdentityClaimStreamBeforeWhen = null!;
    private UserId _canceledUserId = null!;

    protected override async Task Given()
    {
        _canceledUserOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _canceledUserId = new UserId(2.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _canceledUserOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithExternalIdentityId(_subject)
                .WithUserId(_canceledUserId)
                .Build(),
            DomainEvent.UserOnboardingCanceled().Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed().WithUserOnboardingId(_canceledUserOnboardingId).Build(),
            DomainEvent.UserLoginReleased().WithUserOnboardingId(_canceledUserOnboardingId).Build());

        _externalIdentityClaimStreamBeforeWhen = await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(new UserLoginClaimingId(_subject))
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        _name = "Retry";
        _lastName = "Attempt";
        _email = "retry.attempt@test.com";

        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .WithPersonalDetails(_name, _lastName, _email, true)
            .Build();

        _response = await Host.HttpClient.PostJson(
            "/user-onboarding/initiate",
            new InitiateUserOnboardingRequest(),
            token);

        _body = await _response.ReadJson<InitiateUserOnboardingResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_a_new_user_onboarding_id()
        => await Assert.That(_body.UserOnboardingId).IsNotEqualTo(_canceledUserOnboardingId.Value);
    
    [Test]
    public async Task Then_response_returns_a_new_user_id()
        => await Assert.That(_body.UserId).IsNotEqualTo(_canceledUserId.Value);

    [Test]
    public async Task Then_external_identity_is_claimed_for_the_new_user_onboarding()
        => await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(new UserLoginClaimingId(_subject))
            .ShouldAppendExactly(
                _externalIdentityClaimStreamBeforeWhen,
                DomainEvent.UserLoginClaimed()
                    .WithUserId(_body.UserId)
                    .WithUserOnboardingId(_body.UserOnboardingId)
                    .Build());

    [Test]
    public async Task Then_personal_data_is_recorded_for_the_new_user()
        => await Host.Events
            .Stream<UserPersonalDataState, UserPersonalDataId>(new UserPersonalDataId(_body.UserId))
            .ShouldContainExactly(
                DomainEvent.UserPersonalDataRecorded()
                    .WithName(_name)
                    .WithLastName(_lastName)
                    .WithEmail(_email)
                    .WithIsEmailVerified(true)
                    .Build());

    [Test]
    public async Task Then_user_login_is_linked_to_the_new_user()
        => await Host.Events
            .Stream<UserLoginLinkingState, UserLoginLinkingId>(new UserLoginLinkingId(_body.UserId))
            .ShouldContainExactly(
                DomainEvent.UserLoginLinked()
                    .WithUserLoginKey(_subject)
                    .Build());

    [Test]
    public async Task Then_user_onboarding_is_initiated_for_the_new_user()
        => await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(new UserOnboardingId(_body.UserOnboardingId))
            .ShouldContainExactly(
                DomainEvent.UserOnboardingInitiated()
                    .WithUserId(_body.UserId)
                    .Build());
}
