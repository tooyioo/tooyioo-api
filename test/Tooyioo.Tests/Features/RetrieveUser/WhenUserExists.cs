using System.Net;
using Tooyioo.Common;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.RetrieveUser.Contract;
using Tooyioo.User.Features.Support;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.InitiateUserOnboarding.Support;

namespace Tooyioo.Tests.Features.RetrieveUser;

public sealed class WhenUserExists(MongoTestContainer mongoDb)
    : QueryVerticalSliceTest(mongoDb)
{
    private readonly DateTime _createdAtUtc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private HttpResponseMessage _response = null!;
    private RetrieveUserResponse _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private string _userId = null!;
    private string _subject = null!;
    private string _termsAndConditionsVersion = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _userId = 2.ToGuid().ToString();
        _subject = "google-sub-123";
        _termsAndConditionsVersion = "v1";

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(_subject),
            DomainEvent.ExternalIdentityClaimed()
                .WithUserOnboardingId(_userOnboardingId)
                .Build());

        await Host.GivenAndProject<UserProjector, UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            _createdAtUtc,
            DomainEvent.UserOnboardingInitiated()
                .WithName("Jane")
                .WithLastName("Bloggs")
                .WithEmail("jane.bloggs@test.com")
                .WithIsEmailVerified(true)
                .WithExternalIdentityId(_subject)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("jane-bloggs")
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithUserId(_userId)
                .WithTermsAndConditionsVersion(_termsAndConditionsVersion)
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.Get($"/users/{_userId}", token);
        _body = await _response.ReadJson<RetrieveUserResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_user_id()
        => await Assert.That(_body.Id).IsEqualTo(_userId);

    [Test]
    public async Task Then_response_returns_expected_details()
    {
        await Assert.That(_body.Name).IsEqualTo("Jane");
        await Assert.That(_body.LastName).IsEqualTo("Bloggs");
        await Assert.That(_body.Email).IsEqualTo("jane.bloggs@test.com");
        await Assert.That(_body.IsEmailVerified).IsTrue();
        await Assert.That(_body.Alias).IsEqualTo("jane-bloggs");
        await Assert.That(_body.ExternalIdentityId).IsEqualTo(_subject);
        await Assert.That(_body.ExternalIdentityProvider).IsEqualTo(nameof(ExternalIdentityProvider.Google));
        await Assert.That(_body.UserOnboardingId).IsEqualTo(_userOnboardingId.Value);
        await Assert.That(_body.TermsAndConditionsVersion).IsEqualTo(_termsAndConditionsVersion);
    }

    [Test]
    public async Task Then_response_returns_projection_timestamps()
    {
        await Assert.That(_body.CreatedAt).IsEqualTo(_createdAtUtc);
        await Assert.That(_body.LastModifiedAt).IsEqualTo(_createdAtUtc);
    }
}
