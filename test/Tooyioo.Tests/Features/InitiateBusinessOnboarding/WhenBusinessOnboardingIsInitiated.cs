using System.Net;
using Tooyioo.Business;
using Tooyioo.Business.Features.InitiateBusinessOnboarding.Contracts;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.InitiateBusinessOnboarding;

[Category(TestCategories.HappyPath)]
public sealed class WhenBusinessOnboardingIsInitiated
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private InitiateBusinessOnboardingResponse _body = null!;
    private UserId _userOnboardingId = null!;
    private string _userId = null!;
    private string _subject = null!;
    private string _legalName = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserId(1.ToGuid().ToString());
        _userId = _userOnboardingId.Value;
        _subject = "google-sub-123";
        _legalName = "Acme Ltd";

        await Host.Given<UserOnboardingState, UserId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithExternalIdentityId(_subject)
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithUserId(_userId)
                .WithTermsOfServiceVersion("v1")
                .WithPrivacyPolicyVersion("v1")
                .Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserId(_userOnboardingId)
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PostJson(
            "/businesses",
            new InitiateBusinessOnboardingRequest { LegalName = _legalName },
            token);

        _body = await _response.ReadJson<InitiateBusinessOnboardingResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Created);

    [Test]
    public async Task Then_response_returns_the_location_of_the_new_business()
        => await Assert.That(_response.Headers.Location?.ToString()).IsEqualTo($"/businesses/{_body.BusinessId}");

    [Test]
    public async Task Then_response_contains_business_onboarding_id()
        => await Assert.That(Guid.TryParse(_body.BusinessId, out _)).IsTrue();

    [Test]
    public async Task Then_business_onboarding_events_are_saved()
        => await Host.Events
            .Stream<BusinessOnboardingState, BusinessId>(
                new BusinessId(_body.BusinessId))
            .ShouldContainExactly(
                DomainEvent.BusinessOnboardingInitiated()
                    .WithLegalName(_legalName)
                    .WithInitiatedBy(_userId)
                    .Build());
}
