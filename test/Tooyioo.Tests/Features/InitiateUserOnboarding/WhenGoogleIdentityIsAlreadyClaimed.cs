using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.InitiateUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.InitiateUserOnboarding;

[Category(TestCategories.HappyPath)]
public sealed class WhenGoogleIdentityIsAlreadyClaimed
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private InitiateUserOnboardingResponse _body = null!;
    private UserId _existingUserId = null!;
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
        _existingUserId = new UserId(1.ToGuid().ToString());
        _subject = "google-sub-123";
        _name = "Jane";
        _lastName = "Bloggs";
        _email = "jane.bloggs@test.com";

        await Host.Given<UserOnboardingState, UserId>(
            _existingUserId,
            DomainEvent.UserOnboardingInitiated()
                .WithName(_name)
                .WithLastName(_lastName)
                .WithEmail(_email)
                .WithExternalIdentityId(_subject)
                .Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserId(_existingUserId)
                .Build());

        await Host.Given<UserPersonalDataState, UserId>(
            new UserId(_existingUserId.Value),
            DomainEvent.UserPersonalDataRecorded().Build());

        await Host.Given<UserLoginLinkingState, UserId>(
            new UserId(_existingUserId.Value),
            DomainEvent.UserLoginLinked()
                .WithUserLoginKey(_subject)
                .Build());

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserId>(_existingUserId)
            .CaptureSnapshot();

        _externalIdentityClaimStreamBeforeWhen = await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(
                new UserLoginClaimingId(_subject))
            .CaptureSnapshot();

        _personalDataStreamBeforeWhen = await Host.Events
            .Stream<UserPersonalDataState, UserId>(
                new UserId(_existingUserId.Value))
            .CaptureSnapshot();

        _userLoginLinkingStreamBeforeWhen = await Host.Events
            .Stream<UserLoginLinkingState, UserId>(
                new UserId(_existingUserId.Value))
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .WithPersonalDetails("Retry", "Attempt", "retry.attempt@test.com", true)
            .Build();

        _response = await Host.HttpClient.PostJson(
            "/users",
            new { },
            token);

        _body = await _response.ReadJson<InitiateUserOnboardingResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Created);

    [Test]
    public async Task Then_response_returns_existing_user_onboarding_id()
        => await Assert.That(_body.UserId).IsEqualTo(_existingUserId.Value);

    [Test]
    public async Task Then_user_onboarding_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserOnboardingState, UserId>(_existingUserId)
            .ShouldHaveNoChangesSince(_userOnboardingStreamBeforeWhen);

    [Test]
    public async Task Then_external_identity_claim_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(new UserLoginClaimingId(_subject))
            .ShouldHaveNoChangesSince(_externalIdentityClaimStreamBeforeWhen);

    [Test]
    public async Task Then_personal_data_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserPersonalDataState, UserId>(
                new UserId(_existingUserId.Value))
            .ShouldHaveNoChangesSince(_personalDataStreamBeforeWhen);

    [Test]
    public async Task Then_user_login_linking_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserLoginLinkingState, UserId>(
                new UserId(_existingUserId.Value))
            .ShouldHaveNoChangesSince(_userLoginLinkingStreamBeforeWhen);
}
