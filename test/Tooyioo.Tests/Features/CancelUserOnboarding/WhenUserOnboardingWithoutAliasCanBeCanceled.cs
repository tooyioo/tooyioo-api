using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.CancelUserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.CancelUserOnboarding;

public sealed class WhenUserOnboardingWithoutAliasCanBeCanceled
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
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
        _reason = "Changed mind";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithExternalIdentityId(_subject)
                .Build());

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(_subject),
            DomainEvent.ExternalIdentityClaimed().WithUserOnboardingId(_userOnboardingId).Build());

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
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

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
    public async Task Then_no_alias_claim_stream_is_written()
        => await Host.Events
            .Stream<ClaimingUserAliasState, ClaimingUserAliasId>(new ClaimingUserAliasId(_alias))
            .ShouldHaveNoChangesSince(_aliasClaimStreamBeforeWhen);
}
