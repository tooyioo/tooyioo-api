using System.Net;
using Tooyioo.Tests.Features.Support;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.User.Features.RetrieveUsers.Contract;
using Tooyioo.UserOnboarding;

namespace Tooyioo.Tests.Features.RetrieveUsers;

public sealed class WhenUsersAreRetrievedWithoutFilters(MongoTestContainer mongoDb)
    : UserReadModelQueryTest(mongoDb)
{
    private HttpResponseMessage _response = null!;
    private RetrieveUsersResponse _body = null!;
    private string _janeSubject = null!;

    protected override async Task Given()
    {
        var janeUserOnboardingId = new UserOnboardingId("00000000-0000-0000-0000-000000000101");
        var johnUserOnboardingId = new UserOnboardingId("00000000-0000-0000-0000-000000000102");
        var caseyUserOnboardingId = new UserOnboardingId("00000000-0000-0000-0000-000000000103");

        _janeSubject = "google-sub-jane";
        var johnSubject = "google-sub-john";
        var caseySubject = "google-sub-casey";

        await GivenExternalIdentityClaimingEvents(
            _janeSubject,
            DomainEvent.ExternalIdentityClaimed()
                .WithUserOnboardingId(janeUserOnboardingId)
                .Build());

        await GivenExternalIdentityClaimingEvents(
            johnSubject,
            DomainEvent.ExternalIdentityClaimed()
                .WithUserOnboardingId(johnUserOnboardingId)
                .Build());

        await GivenExternalIdentityClaimingEvents(
            caseySubject,
            DomainEvent.ExternalIdentityClaimed()
                .WithUserOnboardingId(caseyUserOnboardingId)
                .Build());

        await GivenUserOnboardingEventsWereProjected(
            janeUserOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithName("Jane")
                .WithLastName("Bloggs")
                .WithEmail("jane.bloggs@test.com")
                .WithExternalIdentityId(_janeSubject)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("jane-bloggs")
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithUserId("ABCDEF00-0000-0000-0000-000000000001")
                .Build());

        await GivenUserOnboardingEventsWereProjected(
            johnUserOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithName("John")
                .WithLastName("Smith")
                .WithEmail("john.smith@test.com")
                .WithExternalIdentityId(johnSubject)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("john-smith")
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithUserId("abcdef00-0000-0000-0000-000000000002")
                .Build());

        await GivenUserOnboardingEventsWereProjected(
            caseyUserOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithName("Casey")
                .WithLastName("Bloggins")
                .WithEmail("casey.bloggins@test.com")
                .WithExternalIdentityId(caseySubject)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("casey-bloggins")
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithUserId("abcdef00-0000-0000-0000-000000000003")
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_janeSubject)
            .Build();

        _response = await Host.HttpClient.Get("/users?pageNumber=1&pageSize=2", token);
        _body = await _response.ReadJson<RetrieveUsersResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_is_paged()
    {
        await Assert.That(_body.PageNumber).IsEqualTo(1);
        await Assert.That(_body.PageSize).IsEqualTo(2);
        await Assert.That(_body.TotalCount).IsEqualTo(3);
        await Assert.That(_body.TotalPages).IsEqualTo(2);
        await Assert.That(_body.Items).Count().IsEqualTo(2);
    }
}
