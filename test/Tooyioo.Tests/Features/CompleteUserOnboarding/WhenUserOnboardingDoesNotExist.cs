using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.CompleteUserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.CompleteUserOnboarding;

public sealed class WhenUserOnboardingDoesNotExist
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(_subject),
            DomainEvent.ExternalIdentityClaimed()
                .WithUserOnboardingId(_userOnboardingId)
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/user-onboarding/{_userOnboardingId.Value}/complete",
            new CompleteUserOnboardingRequest { TermsAndConditionsVersion = "v1" },
            token);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_conflict()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);

    [Test]
    public async Task Then_response_explains_unexpected_state()
        => await Assert.That(_body.Title).IsEqualTo("onboarding_state_unexpected");
}
