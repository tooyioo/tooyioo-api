using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.RetrieveUser.Contract;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.RetrieveUser;

[Category(TestCategories.HappyPath)]
[Category(TestCategories.MongoDb)]
public sealed class WhenUserExists(MongoTestContainer mongoDb)
    : QueryVerticalSliceTest(mongoDb)
{
    private readonly DateTime _createdAtUtc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private HttpResponseMessage _response = null!;
    private RetrieveUserResponse _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private string _userId = null!;
    private string _subject = null!;
    private string _termsOfServiceVersion = null!;
    private string _privacyPolicyVersion = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _userId = _userOnboardingId.Value;
        _subject = "google-sub-123";
        _termsOfServiceVersion = "v1";
        _privacyPolicyVersion = "1.0.0";

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserOnboardingId(_userOnboardingId)
                .Build());

        await Host.Given<UserPersonalDataState, UserPersonalDataId>(
            new UserPersonalDataId(_userOnboardingId.Value),
            DomainEvent.UserPersonalDataRecorded()
                .WithName("Jane")
                .WithLastName("Bloggs")
                .WithEmail("jane.bloggs@test.com")
                .WithIsEmailVerified(true)
                .Build());

        await Host.GivenAndProject<UserProjector, UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            _createdAtUtc,
            DomainEvent.UserOnboardingInitiated()
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("jane-bloggs")
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithUserId(_userId)
                .WithTermsOfServiceVersion(_termsOfServiceVersion)
                .WithPrivacyPolicyVersion(_privacyPolicyVersion)
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
        await Assert.That(_body.Status).IsEqualTo("completed");
        await Assert.That(_body.TermsOfServiceVersion).IsEqualTo(_termsOfServiceVersion);
        await Assert.That(_body.PrivacyPolicyVersion).IsEqualTo(_privacyPolicyVersion);
    }

    [Test]
    public async Task Then_response_returns_projection_timestamps()
    {
        await Assert.That(_body.CreatedAt).IsEqualTo(_createdAtUtc);
        await Assert.That(_body.LastModifiedAt).IsEqualTo(_createdAtUtc);
    }
}
