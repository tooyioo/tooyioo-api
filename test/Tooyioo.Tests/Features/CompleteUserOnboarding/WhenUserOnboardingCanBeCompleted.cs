using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.CompleteUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.CompleteUserOnboarding;

[Category(TestCategories.HappyPath)]
public sealed class WhenUserOnboardingCanBeCompleted
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private CompleteUserOnboardingResponse _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private string _subject = null!;
    private string _termsOfServiceVersion = null!;
    private string _privacyPolicyVersion = null!;
    private EventStreamSnapshot _userOnboardingStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";
        _termsOfServiceVersion = "v1";
        _privacyPolicyVersion = "1.0.0";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithName("Jane")
                .WithLastName("Bloggs")
                .WithEmail("jane.bloggs@test.com")
                .WithExternalIdentityId(_subject)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("jane-bloggs")
                .Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserOnboardingId(_userOnboardingId)
                .Build());

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/user-onboarding/{_userOnboardingId.Value}/complete",
            new CompleteUserOnboardingRequest
            {
                TermsOfServiceVersion = _termsOfServiceVersion,
                PrivacyPolicyVersion = _privacyPolicyVersion
            },
            token);

        _body = await _response.ReadJson<CompleteUserOnboardingResponse>();
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
    public async Task Then_user_onboarding_is_completed()
        => await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_userOnboardingId)
            .ShouldAppendExactly(
                _userOnboardingStreamBeforeWhen,
                DomainEvent.UserOnboardingCompleted()
                    .WithUserId(_body.UserId)
                    .WithTermsOfServiceVersion(_termsOfServiceVersion)
                    .WithPrivacyPolicyVersion(_privacyPolicyVersion)
                    .Build());
}
