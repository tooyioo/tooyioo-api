using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.CompleteUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.CompleteUserOnboarding;

[Category(TestCategories.Conflict)]
public sealed class WhenUserOnboardingHasNoAlias
    : CommandVerticalSliceTest
{
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;
    private UserId _userOnboardingId = null!;
    private string _subject = null!;
    private EventStreamSnapshot _userOnboardingStreamBeforeWhen = null!;

    protected override async Task Given()
    {
        _userOnboardingId = new UserId(1.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserOnboardingState, UserId>(
            _userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithExternalIdentityId(_subject)
                .Build());

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed()
                .WithUserId(_userOnboardingId)
                .Build());

        _userOnboardingStreamBeforeWhen = await Host.Events
            .Stream<UserOnboardingState, UserId>(_userOnboardingId)
            .CaptureSnapshot();
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject(_subject)
            .Build();

        _response = await Host.HttpClient.PutJson(
            $"/users/{_userOnboardingId.Value}/onboarding/completion",
            new CompleteUserOnboardingRequest
            {
                TermsOfServiceVersion = "v1",
                PrivacyPolicyVersion = "v1"
            },
            token);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_unprocessable_entity()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);

    [Test]
    public async Task Then_response_explains_that_no_alias_was_chosen()
        => await Assert.That(_body.Title).IsEqualTo("onboarding_alias_not_chosen");

    [Test]
    public async Task Then_user_onboarding_stream_has_no_new_events()
        => await Host.Events
            .Stream<UserOnboardingState, UserId>(_userOnboardingId)
            .ShouldHaveNoChangesSince(_userOnboardingStreamBeforeWhen);
}
