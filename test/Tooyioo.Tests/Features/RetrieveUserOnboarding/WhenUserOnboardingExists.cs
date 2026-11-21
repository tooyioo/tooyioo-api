using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.RetrieveUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.RetrieveUserOnboarding;

[Category(TestCategories.HappyPath)]
public sealed class WhenUserOnboardingExists
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private RetrieveUserOnboardingResponse _body = null!;
    private UserId _userId = null!;
    private string _subject = null!;
    private string _alias = null!;
    private string _name = null!;
    private string _lastName = null!;
    private string _email = null!;

    protected override async Task Given()
    {
        _userId = new UserId(1.ToGuid().ToString());
        _subject = "google-sub-123";
        _alias = "jane-bloggs";
        _name = "Jane";
        _lastName = "Bloggs";
        _email = "jane.bloggs@test.com";

        await Host.Given<UserOnboardingState, UserId>(
            _userId,
            DomainEvent.UserOnboardingInitiated()
                
                .WithExternalIdentityId(_subject)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias(_alias)
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithTermsOfServiceVersion("1.0.0")
                .WithPrivacyPolicyVersion("1.0.0")
                .Build());

        await Host.Given<UserPersonalDataState, UserId>(
            _userId,
            DomainEvent.UserPersonalDataRecorded()
                .WithName(_name)
                .WithLastName(_lastName)
                .WithEmail(_email)
                .WithIsEmailVerified(true)
                .Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserId(_userId)
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.Get(
            $"/users/{_userId.Value}/onboarding",
            token);

        _body = await _response.ReadJson<RetrieveUserOnboardingResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_the_user_id()
        => await Assert.That(_body.UserId).IsEqualTo(_userId.Value);

    [Test]
    public async Task Then_response_returns_the_onboarding_state()
    {
        await Assert.That(_body.Alias).IsEqualTo(_alias);
        await Assert.That(_body.Status).IsEqualTo("completed");
        await Assert.That(_body.Name).IsEqualTo(_name);
        await Assert.That(_body.LastName).IsEqualTo(_lastName);
        await Assert.That(_body.Email).IsEqualTo(_email);
        await Assert.That(_body.IsEmailVerified).IsTrue();
    }
}
