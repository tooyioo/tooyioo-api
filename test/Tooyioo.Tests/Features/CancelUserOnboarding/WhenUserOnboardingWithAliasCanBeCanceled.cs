using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Contracts;
using Tooyioo.User.Features.CancelUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.CancelUserOnboarding;

[Category(TestCategories.HappyPath)]
public sealed class WhenUserOnboardingWithAliasCanBeCanceled
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private CancelUserOnboardingResponse _body = null!;
    private UserId _userOnboardingId = null!;
    private string _subject = null!;
    private string _alias = null!;
    private string _reason = null!;
    private EventStreamSnapshot _userOnboardingStreamBeforeWhen = null!;
    private EventStreamSnapshot _externalIdentityClaimStreamBeforeWhen = null!;
    private EventStreamSnapshot _aliasClaimStreamBeforeWhen = null!;
    private EventStreamSnapshot _userLoginLinkingStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserId(1.ToGuid().ToString());
        _subject = "google-sub-123";
        _alias = "jane-bloggs";
        _reason = "Terms and Conditions rejected";

        await Host.Given<UserOnboardingState, UserId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithExternalIdentityId(_subject)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen().WithAlias(_alias).Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed().WithUserId(_userOnboardingId).Build());

        await Host.Given<UserAliasClaimingState, UserAliasClaimingId>(
            new UserAliasClaimingId(_alias),
            DomainEvent.UserAliasClaimed().WithUserId(_userOnboardingId).Build());

        await Host.GivenUserPersonalData(_userOnboardingId);
        await Host.GivenLinkedLogin(_userOnboardingId, _subject);

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserId>(_userOnboardingId)
            .CaptureSnapshot();

        _externalIdentityClaimStreamBeforeWhen = await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(new UserLoginClaimingId(_subject))
            .CaptureSnapshot();

        _aliasClaimStreamBeforeWhen = await Host.Events
            .Stream<UserAliasClaimingState, UserAliasClaimingId>(new UserAliasClaimingId(_alias))
            .CaptureSnapshot();

        _userLoginLinkingStreamBeforeWhen = await Host.Events
            .Stream<UserLoginLinkingState, UserId>(new UserId(_userOnboardingId.Value))
            .CaptureSnapshot();

        await Host.Events
            .Stream<UserPersonalDataState, UserId>(new UserId(_userOnboardingId.Value))
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PutJson(
            $"/users/{_userOnboardingId.Value}/onboarding/cancellation",
            new CancelUserOnboardingRequest { ReasonCode = _reason },
            token);

        _body = await _response.ReadJson<CancelUserOnboardingResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_user_onboarding_id()
        => await Assert.That(_body.UserId).IsEqualTo(_userOnboardingId.Value);

    [Test]
    public async Task Then_user_onboarding_is_canceled()
        => await Host.Events
            .Stream<UserOnboardingState, UserId>(_userOnboardingId)
            .ShouldAppendExactly(
                _userOnboardingStreamBeforeWhen,
                DomainEvent.UserOnboardingCanceled().WithReason(_reason).Build());

    [Test]
    public async Task Then_external_identity_is_released()
        => await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(new UserLoginClaimingId(_subject))
            .ShouldAppendExactly(
                _externalIdentityClaimStreamBeforeWhen,
                DomainEvent.UserLoginReleased().WithUserId(_userOnboardingId).Build());

    [Test]
    public async Task Then_user_alias_is_released()
        => await Host.Events
            .Stream<UserAliasClaimingState, UserAliasClaimingId>(new UserAliasClaimingId(_alias))
            .ShouldAppendExactly(
                _aliasClaimStreamBeforeWhen,
                DomainEvent.UserAliasReleased().WithUserId(_userOnboardingId).Build());

    [Test]
    public async Task Then_user_login_is_unlinked()
        => await Host.Events
            .Stream<UserLoginLinkingState, UserId>(new UserId(_userOnboardingId.Value))
            .ShouldAppendExactly(
                _userLoginLinkingStreamBeforeWhen,
                DomainEvent.UserLoginUnlinked().WithUserLoginKey(_subject).Build());

    [Test]
    public async Task Then_personal_data_erasure_is_requested()
        => await Host.Events
            .Stream<UserPersonalDataState, UserId>(new UserId(_userOnboardingId.Value))
            .ShouldContain<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureRequested>();
}
