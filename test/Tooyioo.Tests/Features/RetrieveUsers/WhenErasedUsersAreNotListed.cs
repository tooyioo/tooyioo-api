using System.Net;
using Tooyioo.Tests.Features.Support;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.User.Features.RetrieveUsers.Contract;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.RetrieveUsers;

[Category(TestCategories.HappyPath)]
[Category(TestCategories.MongoDb)]
public sealed class WhenErasedUsersAreNotListed(MongoTestContainer mongoDb)
    : UserReadModelQueryTest(mongoDb)
{
    private HttpResponseMessage _response = null!;
    private RetrieveUsersResponse _body = null!;
    private UserOnboardingId _activeUserId = null!;
    private UserOnboardingId _erasedUserId = null!;

    protected override async Task Given()
    {
        _activeUserId = new UserOnboardingId(1.ToGuid().ToString());
        _erasedUserId = new UserOnboardingId(2.ToGuid().ToString());

        await GivenUserLoginClaimingEvents(
            "google-sub-active",
            DomainEvent.UserLoginClaimed()
                .WithUserOnboardingId(_activeUserId)
                .Build());

        await GivenUserLoginClaimingEvents(
            "google-sub-erased",
            DomainEvent.UserLoginClaimed()
                .WithUserOnboardingId(_erasedUserId)
                .Build());

        await GivenUserOnboardingEventsWereProjected(
            _activeUserId,
            DomainEvent.UserPersonalDataRecorded()
                .WithName("Jane")
                .WithLastName("Bloggs")
                .WithEmail("jane.bloggs@test.com")
                .WithExternalIdentityId("google-sub-active")
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("jane-bloggs")
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithTermsOfServiceVersion("1.0.0")
                .WithPrivacyPolicyVersion("1.0.0")
                .Build());

        await GivenUserOnboardingEventsWereProjected(
            _erasedUserId,
            DomainEvent.UserPersonalDataRecorded()
                .WithName("John")
                .WithLastName("Smith")
                .WithEmail("john.smith@test.com")
                .WithExternalIdentityId("google-sub-erased")
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("john-smith")
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithTermsOfServiceVersion("1.0.0")
                .WithPrivacyPolicyVersion("1.0.0")
                .Build());

        await Host.GivenAndProject<UserProjector, UserPersonalDataState, UserPersonalDataId>(
            new UserPersonalDataId(_erasedUserId.Value),
            DomainEvent.UserPersonalDataErasureApplied().Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject("google-sub-active")
            .Build();

        _response = await Host.HttpClient.Get("/users", token);
        _body = await _response.ReadJson<RetrieveUsersResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_only_the_active_user_is_listed()
    {
        await Assert.That(_body.Items.Any(item => item.Id == _activeUserId.Value)).IsTrue();
        await Assert.That(_body.Items.Any(item => item.Id == _erasedUserId.Value)).IsFalse();
    }
}
