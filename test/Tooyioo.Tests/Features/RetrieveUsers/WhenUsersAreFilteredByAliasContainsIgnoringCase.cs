using System.Net;
using Tooyioo.Tests.Features.Support;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.User.Features.RetrieveUsers.Contract;
using Tooyioo.UserOnboarding;

namespace Tooyioo.Tests.Features.RetrieveUsers;

public sealed class WhenUsersAreFilteredByAliasContainsIgnoringCase(MongoTestContainer mongoDb)
    : UserReadModelQueryTest(mongoDb)
{
    private HttpResponseMessage _response = null!;
    private RetrieveUsersResponse _body = null!;
    private string _janeUserId = null!;
    private string _johnUserId = null!;
    private string _caseyUserId = null!;
    private string _janeSubject = null!;

    protected override async Task Given()
    {
        var janeUserOnboardingId = new UserOnboardingId(101.ToGuid().ToString());
        var johnUserOnboardingId = new UserOnboardingId(102.ToGuid().ToString());
        var caseyUserOnboardingId = new UserOnboardingId(103.ToGuid().ToString());

        _janeUserId = 201.ToGuid().ToString();
        _johnUserId = 202.ToGuid().ToString();
        _caseyUserId = 301.ToGuid().ToString();
        _janeSubject = "google-sub-jane";
        const string johnSubject = "google-sub-john";
        const string caseySubject = "google-sub-casey";

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
                .WithIsEmailVerified(true)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("jane-bloggs")
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithUserId(_janeUserId)
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
                .WithUserId(_johnUserId)
                .Build());

        await GivenUserOnboardingEventsWereProjected(
            caseyUserOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithName("Casey")
                .WithLastName("Bloggins")
                .WithEmail("casey.bloggins@test.com")
                .WithExternalIdentityId(caseySubject)
                .WithIsEmailVerified(true)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("casey-bloggins")
                .Build(),
            DomainEvent.UserOnboardingCompleted()
                .WithUserId(_caseyUserId)
                .Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_janeSubject)
            .Build();

        _response = await Host.HttpClient.Get("/users?alias=BLOGG", token);
        _body = await _response.ReadJson<RetrieveUsersResponse>();
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    [Test]
    public async Task Then_response_contains_matching_users()
    {
        var items = _body.Items.ToArray();

        await Assert.That(items.Length).IsEqualTo(2);
        await Assert.That(items.Any(x => x.Id == _janeUserId)).IsTrue();
        await Assert.That(items.Any(x => x.Id == _caseyUserId)).IsTrue();
        await Assert.That(items.Any(x => x.Id == _johnUserId)).IsFalse();
    }
}
