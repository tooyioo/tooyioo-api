using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.CompleteUserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.InitiateUserOnboarding.Support;

namespace Tooyioo.Tests.Features.CompleteUserOnboarding;

public sealed class WhenAnotherUserTriesToCompleteUserOnboarding
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserOnboardingId _ownerUserOnboardingId = null!;
    private UserOnboardingId _anotherUserOnboardingId = null!;
    private string _ownerSubject = null!;
    private string _anotherSubject = null!;

    protected override async Task Given()
    {
        _ownerUserOnboardingId = new UserOnboardingId(1.ToGuid().ToString());
        _anotherUserOnboardingId = new UserOnboardingId(2.ToGuid().ToString());
        _ownerSubject = "owner-google-sub-123";
        _anotherSubject = "another-google-sub-456";

        await GivenUserOnboarding(_ownerUserOnboardingId, _ownerSubject, "Jane", "Bloggs", "jane.bloggs@test.com");
        await GivenUserOnboarding(_anotherUserOnboardingId, _anotherSubject, "Other", "User", "other.user@test.com");
    }

    protected override async Task When()
    {
        var anotherUserToken = Host.GoogleIdentityToken()
            .WithSubject(_anotherSubject)
            .WithPersonalDetails("Other", "User", "other.user@test.com", true)
            .Build();

        _response = await Host.HttpClient.PostJson(
            $"/user-onboarding/{_ownerUserOnboardingId.Value}/complete",
            new CompleteUserOnboardingRequest { TermsAndConditionsVersion = "v1" },
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

    private async Task GivenUserOnboarding(
        UserOnboardingId userOnboardingId,
        string subject,
        string name,
        string lastName,
        string email)
    {
        await Host.Given<UserOnboardingState, UserOnboardingId>(
            userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithName(name)
                .WithLastName(lastName)
                .WithEmail(email)
                .WithExternalIdentityId(subject)
                .WithIsEmailVerified(true)
                .Build(),
            DomainEvent.UserOnboardingAliasChosen()
                .WithAlias("jane-bloggs")
                .Build());

        await Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(subject),
            DomainEvent.ExternalIdentityClaimed()
                .WithUserOnboardingId(userOnboardingId)
                .Build());
    }
}
