using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.CancelUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.CancelUserOnboarding;

[Category(TestCategories.Conflict)]
public sealed class WhenUserOnboardingIsCompleted
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
            DomainEvent.UserOnboardingCompleted().Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed().WithUserOnboardingId(_userOnboardingId).Build());

        await Host.Given<UserAliasClaimingState, UserAliasClaimingId>(
            new UserAliasClaimingId(_alias),
            DomainEvent.UserAliasClaimed().WithUserOnboardingId(_userOnboardingId).Build());

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .CaptureSnapshot();

        _externalIdentityClaimStreamBeforeWhen = await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(new UserLoginClaimingId(_subject))
            .CaptureSnapshot();

        _aliasClaimStreamBeforeWhen = await Host.Events
            .Stream<UserAliasClaimingState, UserAliasClaimingId>(new UserAliasClaimingId(_alias))
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/user-onboarding/{_userOnboardingId.Value}/cancel",
            new CancelUserOnboardingRequest { ReasonCode = "Changed mind" },
            token);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_unprocessable_entity()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);

    [Test]
    public async Task Then_response_explains_onboarding_is_already_completed()
        => await Assert.That(_body.Title).IsEqualTo("onboarding_already_completed");

    [Test]
    public async Task Then_user_onboarding_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .ShouldHaveNoChangesSince(_userOnboardingStreamBeforeWhen);

    [Test]
    public async Task Then_external_identity_claim_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(new UserLoginClaimingId(_subject))
            .ShouldHaveNoChangesSince(_externalIdentityClaimStreamBeforeWhen);

    [Test]
    public async Task Then_user_alias_claim_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserAliasClaimingState, UserAliasClaimingId>(new UserAliasClaimingId(_alias))
            .ShouldHaveNoChangesSince(_aliasClaimStreamBeforeWhen);
}
