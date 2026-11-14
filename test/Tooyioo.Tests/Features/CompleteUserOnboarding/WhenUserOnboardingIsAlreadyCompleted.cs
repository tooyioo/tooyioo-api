using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.CompleteUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.CompleteUserOnboarding;

[Category(TestCategories.HappyPath)]
public sealed class WhenUserOnboardingIsAlreadyCompleted
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private CompleteUserOnboardingResponse _body = null!;
    private UserId _userOnboardingId = null!;
    private string _subject = null!;
    private EventStreamSnapshot _userOnboardingStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserId(1.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserOnboardingState, UserId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithName("Jane")
                .WithLastName("Bloggs")
                .WithEmail("jane.bloggs@test.com")
                .WithExternalIdentityId(_subject)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("jane-bloggs")
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithUserId(_userOnboardingId.Value)
                .WithTermsOfServiceVersion("v1")
                .WithPrivacyPolicyVersion("v1")
                .Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserId(_userOnboardingId)
                .Build());

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserId>(_userOnboardingId)
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PutJson(
            $"/users/{_userOnboardingId.Value}/onboarding/completion",
            new CompleteUserOnboardingRequest
            {
                TermsOfServiceVersion = "v1",
                PrivacyPolicyVersion = "v1"
            },
            token);

        _body = await _response.ReadJson<CompleteUserOnboardingResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_user_onboarding_id()
        => await Assert.That(_body.UserId).IsEqualTo(_userOnboardingId.Value);

    [Test]
    public async Task Then_user_onboarding_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserOnboardingState, UserId>(_userOnboardingId)
            .ShouldHaveNoChangesSince(_userOnboardingStreamBeforeWhen);
}
