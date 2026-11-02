using System.Net;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Contracts;
using Tooyioo.User.Features.CancelUserPersonalDataErasure.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.UserPersonalData;

[Category(TestCategories.HappyPath)]
// HCL scenario: cancel_user_personal_data_erasure/scenario_01_pending_erasure_is_canceled
public sealed class WhenUserPersonalDataErasureIsCanceled
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private CancelUserPersonalDataErasureResponse _body = null!;
    private UserOnboardingId _userId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _userId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed().WithUserId(_userId.Value).Build());

        await Host.Given<UserPersonalDataState, UserPersonalDataId>(
            new UserPersonalDataId(_userId.Value),
            DomainEvent.UserPersonalDataRecorded().Build(),
            DomainEvent.UserPersonalDataErasureRequested().Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/users/{_userId.Value}/personal-data/cancel-erasure",
            new CancelUserPersonalDataErasureRequest(),
            token);

        _body = await _response.ReadJson<CancelUserPersonalDataErasureResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_returns_the_active_status()
    {
        await Assert.That(_body.UserId).IsEqualTo(_userId.Value);
        await Assert.That(_body.Status).IsEqualTo("active");
    }

    [Test]
    public async Task Then_the_erasure_request_is_canceled()
        => await Host.Events
            .Stream<UserPersonalDataState, UserPersonalDataId>(new UserPersonalDataId(_userId.Value))
            .ShouldContain<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureCanceled>();
}
