using System.Net;
using Slicent.Application;
using Tooyioo.BusinessOnboarding;
using Tooyioo.Common;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.RetrieveBusinessOnboarding;

public sealed class WhenBusinessOnboardingIsNotProjected(MongoTestContainer mongoDb)
    : QueryVerticalSliceTest(mongoDb)
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private BusinessOnboardingId _businessOnboardingId = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private string _userId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _businessOnboardingId = new BusinessOnboardingId(1.ToGuid().ToString());
        _userOnboardingId = new UserOnboardingId(2.ToGuid().ToString());
        _userId = 3.ToGuid().ToString();
        _subject = "google-sub-123";

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(_subject),
            DomainEvent.ExternalIdentityClaimed()
                .WithUserOnboardingId(_userOnboardingId)
                .Build());

        await Host.Given<UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithExternalIdentityId(_subject)
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithUserId(_userId)
                .Build());

        // The business onboarding event exists, but it is intentionally not projected.
        await Host.Given<BusinessOnboardingState, BusinessOnboardingId>(
            _businessOnboardingId,
            DomainEvent.BusinessOnboardingInitiated()
                .WithLegalName("Acme Ltd")
                .WithInitiatedBy(new UserId(_userId))
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.Get(
            $"/business-onboarding/{_businessOnboardingId.Value}",
            token);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_not_found()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

    [Test]
    public async Task Then_response_explains_business_onboarding_not_found()
        => await Assert.That(_body.Title).IsEqualTo("business_onboarding_not_found");
}
