using System.Net;
using Tooyioo.BusinessOnboarding;
using Tooyioo.BusinessOnboarding.Features.RetrieveBusinessOnboarding.Contracts;
using Tooyioo.BusinessOnboarding.Features.Support;
using Tooyioo.Common;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.RetrieveBusinessOnboarding;

[Category(TestCategories.HappyPath)]
[Category(TestCategories.MongoDb)]
public sealed class WhenBusinessOnboardingExists(MongoTestContainer mongoDb)
    : QueryVerticalSliceTest(mongoDb)
{
    private readonly DateTime _createdAtUtc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private HttpResponseMessage _response = null!;
    private RetrieveBusinessOnboardingResponse _body = null!;
    private BusinessOnboardingId _businessOnboardingId = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private string _userId = null!;
    private string _subject = null!;
    private string _legalName = null!;

    protected override async Task Given()
    {
        _businessOnboardingId = new BusinessOnboardingId(1.ToGuid().ToString());
        _userOnboardingId = new UserOnboardingId(2.ToGuid().ToString());
        _userId = 3.ToGuid().ToString();
        _subject = "google-sub-123";
        _legalName = "Acme Ltd";

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

        await Host.GivenAndProject<BusinessOnboardingProjector, BusinessOnboardingState, BusinessOnboardingId>(
            _businessOnboardingId,
            _createdAtUtc,
            DomainEvent.BusinessOnboardingInitiated()
                .WithLegalName(_legalName)
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

        _body = await _response.ReadJson<RetrieveBusinessOnboardingResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_business_onboarding_id()
        => await Assert.That(_body.BusinessOnboardingId).IsEqualTo(_businessOnboardingId.Value);

    [Test]
    public async Task Then_response_returns_legal_name_and_owner()
    {
        await Assert.That(_body.LegalName).IsEqualTo(_legalName);
        await Assert.That(_body.InitiatedBy).IsEqualTo(_userId);
    }

    [Test]
    public async Task Then_response_returns_in_progress_status()
        => await Assert.That(_body.Status).IsEqualTo("in_progress");

    [Test]
    public async Task Then_response_returns_projection_timestamps()
    {
        await Assert.That(_body.CreatedAt).IsEqualTo(_createdAtUtc);
        await Assert.That(_body.LastModifiedAt).IsEqualTo(_createdAtUtc);
    }

    [Test]
    public async Task Then_response_has_no_terminal_dates()
    {
        await Assert.That(_body.CompletedAt).IsNull();
        await Assert.That(_body.CanceledAt).IsNull();
        await Assert.That(_body.CancellationReason).IsNull();
    }
}
