using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Http;
using Tooyioo.User;
using Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.ChooseUserOnboardingAlias;

[Category(TestCategories.Validation)]
public sealed class ChooseUserOnboardingAliasValidationTests
{
    [Test]
    [Arguments("too-short", "ab", "alias_too_short")]
    [Arguments("not-provided", null, "alias_required")]
    [Arguments("too-long", "abcdefghijklmnopqrstuvwxyz12345", "alias_too_long")]
    [Arguments("invalid-format", "jane.bloggs", "alias_invalid_format")]
    [Arguments("uppercase", "Jane", "alias_invalid_format")]
    [Arguments("too-long-and-invalid-format", "abcdefghijklmnopqrstuvwxyz12345!", "alias_too_long", "alias_invalid_format")]
    public async Task Endpoint_rejects_invalid_payloads(
        string _,
        string? alias,
        params string[] expectedErrors)
    {
        await using var host = await VerticalSliceTestHost.Start();

        const string subject = "google-sub-123";
        const string name = "Jane";
        const string lastName = "Bloggs";
        const string email = "jane.bloggs@test.com";
        var userOnboardingId = new UserOnboardingId(1.ToGuid().ToString());

        await host.Given<UserOnboardingState, UserOnboardingId>(
            userOnboardingId,
            DomainEvent.UserOnboardingInitiated()
                .WithName(name)
                .WithLastName(lastName)
                .WithEmail(email)
                .WithExternalIdentityId(subject)
                .Build());

        await host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(subject),
            DomainEvent.UserLoginClaimed()
                .WithUserOnboardingId(userOnboardingId)
                .Build());

        var userOnboardingStreamBeforeWhen = await host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(userOnboardingId)
            .CaptureSnapshot();

        var token = host.GoogleIdentityToken()
            .WithSubject(subject)
            .WithPersonalDetails(name, lastName, email, true)
            .Build();

        object request = alias is null
            ? new { }
            : new ChooseUserOnboardingAliasRequest { Alias = alias };

        using var response = await host.HttpClient.PostJson(
            $"/user-onboarding/{userOnboardingId.Value}/choose-alias",
            request,
            token);

        var body = await response.ReadJson<HttpProblemDetails>();
        var aliasErrors = body.Errors is not null && body.Errors.TryGetValue("alias", out var errors)
            ? errors
            : [];

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(body.Title).IsEqualTo("validation_failed");

        foreach (var expectedError in expectedErrors)
        {
            await Assert.That(aliasErrors.Contains(expectedError)).IsTrue();
        }

        await host.Events
            .Stream<UserOnboardingState, UserOnboardingId>(userOnboardingId)
            .ShouldHaveNoChangesSince(userOnboardingStreamBeforeWhen);
    }
}
