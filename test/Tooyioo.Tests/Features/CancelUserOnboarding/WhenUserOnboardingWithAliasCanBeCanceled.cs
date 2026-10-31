using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.CancelUserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.CancelUserOnboarding;

[Category(TestCategories.HappyPath)]
public sealed class WhenUserOnboardingWithAliasCanBeCanceled
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private CancelUserOnboardingResponse _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private string _subject = null!;
    private string _alias = null!;
    private string _reason = null!;
    private EventStreamSnapshot _userOnboardingStreamBeforeWhen = null!;
    private EventStreamSnapshot _externalIdentityClaimStreamBeforeWhen = null!;
    private EventStreamSnapshot _aliasClaimStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";
        _alias = "jane-bloggs";
        _reason = "Terms and Conditions rejected";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithExternalIdentityId(_subject)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen().WithAlias(_alias).Build());

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(_subject),
            DomainEvent.ExternalIdentityClaimed().WithUserOnboardingId(_userOnboardingId).Build());

        await Host.Given<ClaimingUserAliasState, ClaimingUserAliasId>(
            new ClaimingUserAliasId(_alias),
            DomainEvent.UserAliasClaimed().WithUserOnboardingId(_userOnboardingId).Build());

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .CaptureSnapshot();

        _externalIdentityClaimStreamBeforeWhen = await Host.Events
            .Stream<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(new ClaimingExternalIdentityId(_subject))
            .CaptureSnapshot();

        _aliasClaimStreamBeforeWhen = await Host.Events
            .Stream<ClaimingUserAliasState, ClaimingUserAliasId>(new ClaimingUserAliasId(_alias))
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/user-onboarding/{_userOnboardingId.Value}/cancel",
            new CancelUserOnboardingRequest { Reason = _reason },
            token);

        _body = await _response.ReadJson<CancelUserOnboardingResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_user_onboarding_id()
        => await Assert.That(_body.UserOnboardingId).IsEqualTo(_userOnboardingId.Value);

    [Test]
    public async Task Then_user_onboarding_is_canceled()
        => await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .ShouldAppendExactly(
                _userOnboardingStreamBeforeWhen,
                DomainEvent.UserOnboardingCanceled().WithReason(_reason).Build());

    [Test]
    public async Task Then_external_identity_is_released()
        => await Host.Events
            .Stream<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(new ClaimingExternalIdentityId(_subject))
            .ShouldAppendExactly(
                _externalIdentityClaimStreamBeforeWhen,
                DomainEvent.ExternalIdentityReleased().WithUserOnboardingId(_userOnboardingId).Build());

    [Test]
    public async Task Then_user_alias_is_released()
        => await Host.Events
            .Stream<ClaimingUserAliasState, ClaimingUserAliasId>(new ClaimingUserAliasId(_alias))
            .ShouldAppendExactly(
                _aliasClaimStreamBeforeWhen,
                DomainEvent.UserAliasReleased().WithUserOnboardingId(_userOnboardingId).Build());
}
