using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.ChooseUserOnboardingAlias;

[Category(TestCategories.HappyPath)]
public sealed class WhenUserOnboardingAliasWasReleased
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private UserId _canceledUserId = null!;
    private UserId _newUserId = null!;
    private string _canceledSubject = null!;
    private string _newSubject = null!;
    private string _alias = null!;
    private EventStreamSnapshot _newUserOnboardingStreamBeforeWhen = null!;
    private EventStreamSnapshot _aliasClaimStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _canceledUserId = new UserId(1.ToGuid().ToString());
        _newUserId = new UserId(2.ToGuid().ToString());
        _canceledSubject = "canceled-google-sub-123";
        _newSubject = "new-google-sub-456";
        _alias = "jane-bloggs";

        await Host.Given<UserOnboardingState, UserId>(
            _canceledUserId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(_canceledSubject).Build(),
            DomainEvent.UserOnboardingAliasChosen().WithAlias(_alias).Build(),
            DomainEvent.UserOnboardingCanceled().Build());

        await Host.Given<UserOnboardingState, UserId>(
            _newUserId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(_newSubject).Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_newSubject),
            DomainEvent.UserLoginClaimed().WithUserId(_newUserId).Build());

        await Host.Given<UserAliasClaimingState, UserAliasClaimingId>(
            new UserAliasClaimingId(_alias),
            DomainEvent.UserAliasClaimed().WithUserId(_canceledUserId).Build(),
            DomainEvent.UserAliasReleased().WithUserId(_canceledUserId).Build());

        _newUserOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserId>(_newUserId)
            .CaptureSnapshot();

        _aliasClaimStreamBeforeWhen = await Host.Events
            .Stream<UserAliasClaimingState, UserAliasClaimingId>(new UserAliasClaimingId(_alias))
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_newSubject)
            .Build();

        _response = await Host.HttpClient.PutJson(
            $"/users/{_newUserId.Value}/onboarding/alias",
            new ChooseUserOnboardingAliasRequest { Alias = _alias },
            token);
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_new_user_onboarding_chooses_alias()
        => await Host.Events
            .Stream<UserOnboardingState, UserId>(_newUserId)
            .ShouldAppendExactly(
                _newUserOnboardingStreamBeforeWhen,
                DomainEvent.UserOnboardingAliasChosen().WithAlias(_alias).Build());

    [Test]
    public async Task Then_alias_is_claimed_by_new_user_onboarding()
        => await Host.Events
            .Stream<UserAliasClaimingState, UserAliasClaimingId>(new UserAliasClaimingId(_alias))
            .ShouldAppendExactly(
                _aliasClaimStreamBeforeWhen,
                DomainEvent.UserAliasClaimed().WithUserId(_newUserId).Build());
}
