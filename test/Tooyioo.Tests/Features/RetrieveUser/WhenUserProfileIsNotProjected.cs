using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.RetrieveUser;

public sealed class WhenUserProfileIsNotProjected(MongoTestContainer mongoDb)
    : QueryVerticalSliceTest(mongoDb)
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private string _userId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _userId = 2.ToGuid().ToString();
        _subject = "google-sub-123";

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(_subject),
            DomainEvent.ExternalIdentityClaimed()
                .WithUserOnboardingId(_userOnboardingId)
                .Build());

        // The external identity claim and completed onboarding supply the app-owned user_id claim.
        // They are intentionally not projected, so the User read model is absent.
        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithExternalIdentityId(_subject)
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithUserId(_userId)
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.Get($"/users/{_userId}", token);
        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_not_found()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

    [Test]
    public async Task Then_response_explains_profile_not_found()
        => await Assert.That(_body.Title).IsEqualTo("profile_not_found");
}
