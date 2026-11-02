using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.InitiateUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.InitiateUserOnboarding;

[Category(TestCategories.HappyPath)]
public sealed class WhenGoogleIdentityIsAlreadyClaimed
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private InitiateUserOnboardingResponse _body = null!;
    private UserOnboardingId _existingUserOnboardingId = null!;
    private string _subject = null!;
    private string _name = null!;
    private string _lastName = null!;
    private string _email = null!;
    private EventStreamSnapshot _userOnboardingStreamBeforeWhen = null!;
    private EventStreamSnapshot _externalIdentityClaimStreamBeforeWhen = null!;
    private EventStreamSnapshot _personalDataStreamBeforeWhen = null!;
    private EventStreamSnapshot _userLoginLinkingStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _existingUserOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";
        _name = "Jane";
        _lastName = "Bloggs";
        _email = "jane.bloggs@test.com";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _existingUserOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithName(_name)
                .WithLastName(_lastName)
                .WithEmail(_email)
                .WithExternalIdentityId(_subject)
                .Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserOnboardingId(_existingUserOnboardingId)
                .Build());

        await Host.Given<UserPersonalDataState, UserPersonalDataId>(
            new UserPersonalDataId(_existingUserOnboardingId.Value),
            DomainEvent.UserPersonalDataRecorded().Build());

        await Host.Given<UserLoginLinkingState, UserLoginLinkingId>(
            new UserLoginLinkingId(_existingUserOnboardingId.Value),
            DomainEvent.UserLoginLinked()
                .WithUserLoginKey(_subject)
                .Build());

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_existingUserOnboardingId)
            .CaptureSnapshot();

        _externalIdentityClaimStreamBeforeWhen = await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(
                new UserLoginClaimingId(_subject))
            .CaptureSnapshot();

        _personalDataStreamBeforeWhen = await Host.Events
            .Stream<UserPersonalDataState, UserPersonalDataId>(
                new UserPersonalDataId(_existingUserOnboardingId.Value))
            .CaptureSnapshot();

        _userLoginLinkingStreamBeforeWhen = await Host.Events
            .Stream<UserLoginLinkingState, UserLoginLinkingId>(
                new UserLoginLinkingId(_existingUserOnboardingId.Value))
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .WithPersonalDetails("Retry", "Attempt", "retry.attempt@test.com", true)
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
    public async Task Then_response_returns_existing_user_onboarding_id()
        => await Assert.That(_body.UserOnboardingId).IsEqualTo(_existingUserOnboardingId.Value);

    [Test]
    public async Task Then_user_onboarding_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_existingUserOnboardingId)
            .ShouldHaveNoChangesSince(_userOnboardingStreamBeforeWhen);

    [Test]
    public async Task Then_external_identity_claim_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(new UserLoginClaimingId(_subject))
            .ShouldHaveNoChangesSince(_externalIdentityClaimStreamBeforeWhen);

    [Test]
    public async Task Then_personal_data_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserPersonalDataState, UserPersonalDataId>(
                new UserPersonalDataId(_existingUserOnboardingId.Value))
            .ShouldHaveNoChangesSince(_personalDataStreamBeforeWhen);

    [Test]
    public async Task Then_user_login_linking_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserLoginLinkingState, UserLoginLinkingId>(
                new UserLoginLinkingId(_existingUserOnboardingId.Value))
            .ShouldHaveNoChangesSince(_userLoginLinkingStreamBeforeWhen);
}
