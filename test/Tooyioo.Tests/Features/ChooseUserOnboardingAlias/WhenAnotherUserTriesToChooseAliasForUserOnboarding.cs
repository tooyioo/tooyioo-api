using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.ChooseUserOnboardingAlias;

[Category(TestCategories.Authorization)]
public sealed class WhenAnotherUserTriesToChooseAliasForUserOnboarding
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserId _ownerUserId = null!;
    private UserId _anotherUserId = null!;
    private string _ownerSubject = null!;
    private string _anotherSubject = null!;
    private string _alias = null!;

    protected override async Task Given()
    {
        _ownerUserId = new UserId(1.ToGuid().ToString());
        _anotherUserId = new UserId(2.ToGuid().ToString());
        _ownerSubject = "owner-google-sub-123";
        _anotherSubject = "another-google-sub-456";
        _alias = "jane-bloggs";

        await Host.Given<UserOnboardingState, UserId>(
            _ownerUserId,
            DomainEvent.UserOnboardingInitiated()
                .WithName("Jane")
                .WithLastName("Bloggs")
                .WithEmail("jane.bloggs@test.com")
                .WithExternalIdentityId(_ownerSubject)
                .Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_ownerSubject),
            DomainEvent.UserLoginClaimed()
                .WithUserId(_ownerUserId)
                .Build());

        await Host.Given<UserOnboardingState, UserId>(
            _anotherUserId,
            DomainEvent.UserOnboardingInitiated()
                .WithName("Other")
                .WithLastName("User")
                .WithEmail("other.user@test.com")
                .WithExternalIdentityId(_anotherSubject)
                .Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_anotherSubject),
            DomainEvent.UserLoginClaimed()
                .WithUserId(_anotherUserId)
                .Build());
    }

    protected override async Task When()
    {
        var anotherUserToken = Host.GoogleIdentityToken()
            .WithSubject(_anotherSubject)
            .WithPersonalDetails("Other", "User", "other.user@test.com", true)
            .Build();

        _response = await Host.HttpClient.PutJson(
            $"/users/{_ownerUserId.Value}/onboarding/alias",
            new ChooseUserOnboardingAliasRequest { Alias = _alias },
            anotherUserToken);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_forbidden()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

    [Test]
    public async Task Then_response_title_is_forbidden()
        => await Assert.That(_body.Title).IsEqualTo("forbidden");

    [Test]
    public async Task Then_response_explains_user_is_not_allowed()
        => await Assert.That(_body.Detail).IsEqualTo("You are not allowed to access this resource.");
}
