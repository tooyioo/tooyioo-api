using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.ChooseUserOnboardingAlias.Contracts;
using Tooyioo.UserOnboarding.Features.ChooseUserOnboardingAlias.Support;
using Tooyioo.UserOnboarding.Features.InitiateUserOnboarding.Support;

namespace Tooyioo.Tests.Features.ChooseUserOnboardingAlias;

public sealed class WhenUserOnboardingAliasWasReleased
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private UserOnboardingId _canceledUserOnboardingId = null!;
    private UserOnboardingId _newUserOnboardingId = null!;
    private string _canceledSubject = null!;
    private string _newSubject = null!;
    private string _alias = null!;
    private EventStreamSnapshot _newUserOnboardingStreamBeforeWhen = null!;
    private EventStreamSnapshot _aliasClaimStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _canceledUserOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _newUserOnboardingId = new UserOnboardingId(2.ToGuid().ToString());
        _canceledSubject = "canceled-google-sub-123";
        _newSubject = "new-google-sub-456";
        _alias = "jane-bloggs";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _canceledUserOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(_canceledSubject).Build(),
            DomainEvent.UserOnboardingAliasChosen().WithAlias(_alias).Build(),
            DomainEvent.UserOnboardingCanceled().Build());

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _newUserOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(_newSubject).Build());

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(_newSubject),
            DomainEvent.ExternalIdentityClaimed().WithUserOnboardingId(_newUserOnboardingId).Build());

        await Host.Given<ClaimingAliasState, ClaimingAliasId>(
            new ClaimingAliasId(_alias),
            DomainEvent.AliasClaimed().WithUserOnboardingId(_canceledUserOnboardingId).Build(),
            DomainEvent.AliasReleased().WithUserOnboardingId(_canceledUserOnboardingId).Build());

        _newUserOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_newUserOnboardingId)
            .CaptureSnapshot();

        _aliasClaimStreamBeforeWhen = await Host.Events
            .Stream<ClaimingAliasState, ClaimingAliasId>(new ClaimingAliasId(_alias))
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_newSubject)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/user-onboarding/{_newUserOnboardingId.Value}/choose-alias",
            new ChooseUserOnboardingAliasRequest { Alias = _alias },
            token);
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_new_user_onboarding_chooses_alias()
        => await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_newUserOnboardingId)
            .ShouldAppendExactly(
                _newUserOnboardingStreamBeforeWhen,
                DomainEvent.UserOnboardingAliasChosen().WithAlias(_alias).Build());

    [Test]
    public async Task Then_alias_is_claimed_by_new_user_onboarding()
        => await Host.Events
            .Stream<ClaimingAliasState, ClaimingAliasId>(new ClaimingAliasId(_alias))
            .ShouldAppendExactly(
                _aliasClaimStreamBeforeWhen,
                DomainEvent.AliasClaimed().WithUserOnboardingId(_newUserOnboardingId).Build());
}
