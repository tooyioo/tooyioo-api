using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.InitiateUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.InitiateUserOnboarding;

[Category(TestCategories.HappyPath)]
public sealed class WhenGoogleIdentityIsUnknown
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private InitiateUserOnboardingResponse _body = null!;
    private string _subject = null!;
    private string _name = null!;
    private string _lastName = null!;
    private string _email = null!;

    protected override Task Given()
        => Task.CompletedTask;

    protected override async Task When()
    {
        _subject = "google-sub-123";
        _name = "Jane";
        _lastName = "Bloggs";
        _email = "jane.bloggs@test.com";
        
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .WithPersonalDetails(_name, _lastName, _email, true)
            .Build();

        _response = await Host.HttpClient.PostJson(
            "/user-onboarding/initiate",
            new { },
            token);

        _body = await _response.ReadJson<InitiateUserOnboardingResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_contains_user_onboarding_id()
        => await Assert.That(Guid.TryParse(_body.UserOnboardingId, out _)).IsTrue();
    
    [Test]
    public async Task Then_response_contains_user_id()
        => await Assert.That(Guid.TryParse(_body.UserId, out _)).IsTrue();

    [Test]
    public async Task Then_user_onboarding_events_are_saved()
        => await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(new UserOnboardingId(_body.UserOnboardingId))
            .ShouldContainExactly(
                DomainEvent.UserOnboardingInitiated()
                    .WithUserId(_body.UserId)
                    .WithName(_name)
                    .WithLastName(_lastName)
                    .WithEmail(_email)
                    .WithIsEmailVerified(true)
                    .WithExternalIdentityId(_subject)
                    .Build());

    [Test]
    public async Task Then_external_identity_is_claimed()
        => await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(new UserLoginClaimingId(_subject))
            .ShouldContainExactly(
                DomainEvent.UserLoginClaimed()
                    .WithUserId(_body.UserId)
                    .WithUserOnboardingId(_body.UserOnboardingId)
                    .Build());

    [Test]
    public async Task Then_personal_data_is_recorded()
        => await Host.Events
            .Stream<UserPersonalDataState, UserPersonalDataId>(new UserPersonalDataId(_body.UserId))
            .ShouldContainExactly(
                DomainEvent.UserPersonalDataRecorded()
                    .WithName(_name)
                    .WithLastName(_lastName)
                    .WithEmail(_email)
                    .WithIsEmailVerified(true)
                    .Build());

    [Test]
    public async Task Then_user_login_is_linked()
        => await Host.Events
            .Stream<UserLoginLinkingState, UserLoginLinkingId>(new UserLoginLinkingId(_body.UserId))
            .ShouldContainExactly(
                DomainEvent.UserLoginLinked()
                    .WithUserLoginKey(_subject)
                    .Build());
}
