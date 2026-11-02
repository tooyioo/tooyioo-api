using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.ChooseUserOnboardingAlias;

[Category(TestCategories.Authorization)]
public sealed class WhenCanceledUserOnboardingChoosesAlias
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private string _subject = null!;
    private string _alias = null!;
    private EventStreamSnapshot _userOnboardingStreamBeforeWhen = null!;
    private EventStreamSnapshot _userAliasClaimStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";
        _alias = "jane-bloggs";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(_subject).Build(),
            DomainEvent.UserOnboardingCanceled().Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed().WithUserOnboardingId(_userOnboardingId).Build(),
            DomainEvent.UserLoginReleased().WithUserOnboardingId(_userOnboardingId).Build());

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .CaptureSnapshot();

        _userAliasClaimStreamBeforeWhen = await Host.Events
            .Stream<UserAliasClaimingState, UserAliasClaimingId>(new UserAliasClaimingId(_alias))
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/user-onboarding/{_userOnboardingId.Value}/choose-alias",
            new ChooseUserOnboardingAliasRequest { Alias = _alias },
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
    public async Task Then_user_alias_claim_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserAliasClaimingState, UserAliasClaimingId>(new UserAliasClaimingId(_alias))
            .ShouldHaveNoChangesSince(_userAliasClaimStreamBeforeWhen);
}
