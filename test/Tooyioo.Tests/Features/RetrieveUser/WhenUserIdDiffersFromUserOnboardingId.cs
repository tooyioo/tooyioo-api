using System.Net;
using Tooyioo.Common;
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
public sealed class WhenUserIdDiffersFromUserOnboardingId(MongoTestContainer mongoDb)
    : QueryVerticalSliceTest(mongoDb)
{
    private HttpResponseMessage _response = null!;
    private RetrieveUserResponse _body = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private UserId _userId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _userId = new UserId(2.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserId(_userId)
                .WithUserOnboardingId(_userOnboardingId)
                .Build());

        await Host.Given<UserPersonalDataState, UserPersonalDataId>(
            new UserPersonalDataId(_userId.Value),
            DomainEvent.UserPersonalDataRecorded()
                .WithName("Joe")
                .WithLastName("Bloggs")
                .WithEmail("joe@test.com")
                .Build());

        await Host.GivenAndProject<UserProjector, UserOnboardingState, UserOnboardingId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated().WithUserId(_userId.Value).Build(),
            DomainEvent.UserOnboardingAliasChosen().WithAlias("joe-bloggs").Build(),
            DomainEvent.UserOnboardingCompleted().Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.Get($"/users/{_userId.Value}", token);
        _body = await _response.ReadJson<RetrieveUserResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_the_user_document_is_keyed_by_user_id()
    {
        await Assert.That(_body.Id).IsEqualTo(_userId.Value);
        await Assert.That(_body.Alias).IsEqualTo("joe-bloggs");
        await Assert.That(_body.Name).IsEqualTo("Joe");
        await Assert.That(_body.Status).IsEqualTo("completed");
    }
}
