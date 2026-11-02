using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.ChooseUserOnboardingAlias;

[Category(TestCategories.HappyPath)]
public sealed class WhenUserOnboardingAliasIsAvailable
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private ChooseUserOnboardingAliasResponse _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
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

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserOnboardingId(_userOnboardingId)
                .Build());

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .CaptureSnapshot();

        _aliasClaimStreamBeforeWhen = await Host.Events
            .Stream<UserAliasClaimingState, UserAliasClaimingId>(new UserAliasClaimingId(_alias))
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

        _body = await _response.ReadJson<ChooseUserOnboardingAliasResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_user_onboarding_id()
        => await Assert.That(_body.UserOnboardingId).IsEqualTo(_userOnboardingId.Value);

    [Test]
    public async Task Then_response_returns_user_id()
        => await Assert.That(Guid.TryParse(_body.UserId, out _)).IsTrue();

    [Test]
    public async Task Then_user_onboarding_alias_is_chosen()
        => await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .ShouldAppendExactly(
                _userOnboardingStreamBeforeWhen,
                DomainEvent.UserOnboardingAliasChosen()
                    .WithAlias(_alias)
                    .Build());

    [Test]
    public async Task Then_user_alias_is_claimed()
        => await Host.Events
            .Stream<UserAliasClaimingState, UserAliasClaimingId>(new UserAliasClaimingId(_alias))
            .ShouldAppendExactly(
                _aliasClaimStreamBeforeWhen,
                DomainEvent.UserAliasClaimed()
                    .WithUserOnboardingId(_userOnboardingId)
                    .Build());
}
