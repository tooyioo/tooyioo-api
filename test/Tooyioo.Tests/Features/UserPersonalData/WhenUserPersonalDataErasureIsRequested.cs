using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Contracts;
using Tooyioo.User.Features.RequestUserPersonalDataErasure.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.UserPersonalData;

[Category(TestCategories.HappyPath)]
// HCL scenario: request_user_personal_data_erasure/scenario_01_user_personal_data_erasure_is_requested
public sealed class WhenUserPersonalDataErasureIsRequested
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private RequestUserPersonalDataErasureResponse _body = null!;
    private UserOnboardingId _userId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _userId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed().WithUserId(_userId.Value).Build());

        await Host.GivenUserPersonalData(_userId, "Joe", "Bloggs", "joe@test.com", true);
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/users/{_userId.Value}/personal-data/erase",
            new RequestUserPersonalDataErasureRequest { ReasonCode = "user_request" },
            token);

        _body = await _response.ReadJson<RequestUserPersonalDataErasureResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_the_pending_erasure()
    {
        await Assert.That(_body.UserId).IsEqualTo(_userId.Value);
        await Assert.That(_body.Status).IsEqualTo("erasure_requested");
        await Assert.That(_body.EraseAfterUtc).IsGreaterThan(DateTime.UtcNow);
    }

    [Test]
    public async Task Then_the_erasure_request_is_appended()
        => await Host.Events
            .Stream<UserPersonalDataState, UserPersonalDataId>(new UserPersonalDataId(_userId.Value))
            .ShouldContain<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureRequested>();
}
