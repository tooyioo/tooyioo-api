using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.InitiateUserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.Support;

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

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(_subject),
            DomainEvent.ExternalIdentityClaimed()
                .WithUserOnboardingId(_existingUserOnboardingId)
                .Build());

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(_existingUserOnboardingId)
            .CaptureSnapshot();

        _externalIdentityClaimStreamBeforeWhen = await Host.Events
            .Stream<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
                new ClaimingExternalIdentityId(_subject))
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
            .Stream<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(new ClaimingExternalIdentityId(_subject))
            .ShouldHaveNoChangesSince(_externalIdentityClaimStreamBeforeWhen);
}
