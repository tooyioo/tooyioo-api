using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.ChooseUserOnboardingAlias.Contracts;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.ChooseUserOnboardingAlias;

[Category(TestCategories.Conflict)]
public sealed class WhenUserOnboardingAliasIsAlreadyInUse
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private UserOnboardingId _otherUserOnboardingId = null!;
    private string _subject = null!;
    private string _name = null!;
    private string _lastName = null!;
    private string _email = null!;
    private string _alias = null!;
    private EventStreamSnapshot _userOnboardingStreamBeforeWhen = null!;
    private EventStreamSnapshot _aliasClaimStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _otherUserOnboardingId = new UserOnboardingId(2.ToGuid().ToString());
        _subject = "google-sub-123";
        _name = "Jane";
        _lastName = "Bloggs";
        _email = "jane.bloggs@test.com";
        _alias = "jane-bloggs";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithName(_name)
                .WithLastName(_lastName)
                .WithEmail(_email)
                .WithExternalIdentityId(_subject)
                .Build());

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(_subject),
            DomainEvent.ExternalIdentityClaimed()
                .WithUserOnboardingId(_userOnboardingId)
                .Build());

        await Host.Given<ClaimingUserAliasState, ClaimingUserAliasId>(
            new ClaimingUserAliasId(_alias),
            DomainEvent.UserAliasClaimed()
                .WithUserOnboardingId(_otherUserOnboardingId)
                .Build());

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .CaptureSnapshot();

        _aliasClaimStreamBeforeWhen = await Host.Events
            .Stream<ClaimingUserAliasState, ClaimingUserAliasId>(new ClaimingUserAliasId(_alias))
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .WithPersonalDetails(_name, _lastName, _email, true)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/user-onboarding/{_userOnboardingId.Value}/choose-alias",
            new ChooseUserOnboardingAliasRequest { Alias = _alias },
            token);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_conflict()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);

    [Test]
    public async Task Then_response_explains_alias_is_already_in_use()
        => await Assert.That(_body.Title).IsEqualTo("alias_already_claimed");

    [Test]
    public async Task Then_user_onboarding_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .ShouldHaveNoChangesSince(_userOnboardingStreamBeforeWhen);

    [Test]
    public async Task Then_user_alias_claim_stream_has_no_new_events()
        => await Host.Events
            .Stream<ClaimingUserAliasState, ClaimingUserAliasId>(new ClaimingUserAliasId(_alias))
            .ShouldHaveNoChangesSince(_aliasClaimStreamBeforeWhen);
}
