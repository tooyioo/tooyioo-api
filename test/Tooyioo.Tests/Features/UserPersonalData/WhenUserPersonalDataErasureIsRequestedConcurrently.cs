using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.RequestUserPersonalDataErasure.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.UserPersonalData;

[Category(TestCategories.Conflict)]
public sealed class WhenUserPersonalDataErasureIsRequestedConcurrently
    : ConcurrentCommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserOnboardingId _userId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _userId = new UserOnboardingId(1.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed().WithUserId(_userId.Value).Build());

        await Host.GivenUserPersonalData(_userId);
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

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_conflict()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);

    [Test]
    public async Task Then_response_title_is_concurrent_update()
        => await Assert.That(_body.Title).IsEqualTo("concurrent_update");
}
