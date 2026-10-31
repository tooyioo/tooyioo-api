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

public sealed class WhenUserOnboardingIsAlreadyCanceled
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private string _subject = null!;
    private string _alias = null!;
    private EventStreamSnapshot _userOnboardingStreamBeforeWhen = null!;
    private EventStreamSnapshot _externalIdentityClaimStreamBeforeWhen = null!;
    private EventStreamSnapshot _aliasClaimStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";
        _alias = "jane-bloggs";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(_subject).Build(),
            DomainEvent.UserOnboardingAliasChosen().WithAlias(_alias).Build(),
            DomainEvent.UserOnboardingCanceled().Build());

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(_subject),
            DomainEvent.ExternalIdentityClaimed().WithUserOnboardingId(_userOnboardingId).Build(),
            DomainEvent.ExternalIdentityReleased().WithUserOnboardingId(_userOnboardingId).Build());

        await Host.Given<ClaimingUserAliasState, ClaimingUserAliasId>(
            new ClaimingUserAliasId(_alias),
            DomainEvent.UserAliasClaimed().WithUserOnboardingId(_userOnboardingId).Build(),
            DomainEvent.UserAliasReleased().WithUserOnboardingId(_userOnboardingId).Build());

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
            new CancelUserOnboardingRequest { Reason = "Retry" },
            token);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_forbidden()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

    [Test]
    public async Task Then_response_title_is_forbidden()
        => await Assert.That(_body.Title).IsEqualTo("forbidden");

    [Test]
    public async Task Then_user_onboarding_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .ShouldHaveNoChangesSince(_userOnboardingStreamBeforeWhen);

    [Test]
    public async Task Then_external_identity_claim_stream_has_no_new_events()
        => await Host.Events
            .Stream<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(new ClaimingExternalIdentityId(_subject))
            .ShouldHaveNoChangesSince(_externalIdentityClaimStreamBeforeWhen);

    [Test]
    public async Task Then_user_alias_claim_stream_has_no_new_events()
        => await Host.Events
            .Stream<ClaimingUserAliasState, ClaimingUserAliasId>(new ClaimingUserAliasId(_alias))
            .ShouldHaveNoChangesSince(_aliasClaimStreamBeforeWhen);
}
