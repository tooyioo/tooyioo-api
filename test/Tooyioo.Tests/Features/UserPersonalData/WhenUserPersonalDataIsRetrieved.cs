using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.RetrieveUserPersonalData.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.UserPersonalData;

[Category(TestCategories.HappyPath)]
[Category(TestCategories.MongoDb)]
// HCL scenario: retrieve_user_personal_data/scenario_01_active_user_personal_data_is_retrieved
public sealed class WhenUserPersonalDataIsRetrieved(MongoTestContainer mongoDb)
    : QueryVerticalSliceTest(mongoDb)
{
    private HttpResponseMessage _response = null!;
    private RetrieveUserPersonalDataResponse _body = null!;
    private UserOnboardingId _userId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _userId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed().WithUserId(_userId.Value).Build());

        await Host.GivenAndProject<UserPersonalDataProjector, UserPersonalDataState, UserPersonalDataId>(
            new UserPersonalDataId(_userId.Value),
            DomainEvent.UserPersonalDataRecorded()
                .WithName("Joe")
                .WithLastName("Bloggs")
                .WithEmail("joe@test.com")
                .WithIsEmailVerified(true)
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.Get($"/users/{_userId.Value}/personal-data", token);
        _body = await _response.ReadJson<RetrieveUserPersonalDataResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_the_user_personal_data()
    {
        await Assert.That(_body.UserId).IsEqualTo(_userId.Value);
        await Assert.That(_body.Name).IsEqualTo("Joe");
        await Assert.That(_body.LastName).IsEqualTo("Bloggs");
        await Assert.That(_body.Email).IsEqualTo("joe@test.com");
        await Assert.That(_body.IsEmailVerified).IsTrue();
        await Assert.That(_body.Status).IsEqualTo("active");
        await Assert.That(_body.EraseAfterUtc).IsNull();
    }
}
