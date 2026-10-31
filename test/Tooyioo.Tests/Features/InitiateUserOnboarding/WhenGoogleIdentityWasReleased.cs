using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.InitiateUserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.InitiateUserOnboarding;

public sealed class WhenGoogleIdentityWasReleased
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private InitiateUserOnboardingResponse _body = null!;
    private UserOnboardingId _canceledUserOnboardingId = null!;
    private string _subject = null!;
    private EventStreamSnapshot _externalIdentityClaimStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _canceledUserOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _canceledUserOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithExternalIdentityId(_subject).Build(),
            DomainEvent.UserOnboardingCanceled().Build());

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(_subject),
            DomainEvent.ExternalIdentityClaimed().WithUserOnboardingId(_canceledUserOnboardingId).Build(),
            DomainEvent.ExternalIdentityReleased().WithUserOnboardingId(_canceledUserOnboardingId).Build());

        _externalIdentityClaimStreamBeforeWhen = await Host.Events
            .Stream<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(new ClaimingExternalIdentityId(_subject))
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
            new InitiateUserOnboardingRequest(),
            token);

        _body = await _response.ReadJson<InitiateUserOnboardingResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_a_new_user_onboarding_id()
        => await Assert.That(_body.UserOnboardingId).IsNotEqualTo(_canceledUserOnboardingId.Value);

    [Test]
    public async Task Then_external_identity_is_claimed_for_the_new_user_onboarding()
        => await Host.Events
            .Stream<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(new ClaimingExternalIdentityId(_subject))
            .ShouldAppendExactly(
                _externalIdentityClaimStreamBeforeWhen,
                DomainEvent.ExternalIdentityClaimed().WithUserOnboardingId(_body.UserOnboardingId).Build());
}
