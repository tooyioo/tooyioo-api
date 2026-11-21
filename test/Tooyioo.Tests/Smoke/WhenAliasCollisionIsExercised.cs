using System.Net;
using Slicent.Application;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.Smoke;
using Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;
using Tooyioo.User.Features.RetrieveUser.Contract;

namespace Tooyioo.Tests.Smoke;

/// <summary>
/// Real alias collision: the first claim goes through KurrentDB and the second is rejected
/// by the alias claim stream, not by an in-memory substitute.
/// </summary>
[Category(TestCategories.Smoke)]
public sealed class WhenAliasCollisionIsExercised(SmokeTestHost host)
    : SmokeScenario(host)
{
    private string _alias = null!;
    private HttpStatusCode _firstAliasStatus;
    private HttpStatusCode _collisionStatus;
    private HttpProblemDetails _collision = null!;
    private RetrieveUserResponse _secondUser = null!;

    protected override async Task Given()
    {
        _alias = Tenant.Alias("shared");

        var token = Host.Token(Tenant.Subject("a"), "Jane", "Bloggs", Tenant.Email("a"));
        var userId = await CreateUserAsync(token);

        using var response = await Host.HttpClient.PutJson(
            $"/users/{userId}/onboarding/alias",
            new ChooseUserOnboardingAliasRequest { Alias = _alias },
            token);
        _firstAliasStatus = response.StatusCode;
    }

    protected override async Task When()
    {
        var token = Host.Token(Tenant.Subject("b"), "John", "Doe", Tenant.Email("b"));
        var userId = await CreateUserAsync(token);

        using var response = await Host.HttpClient.PutJson(
            $"/users/{userId}/onboarding/alias",
            new ChooseUserOnboardingAliasRequest { Alias = _alias },
            token);

        _collisionStatus = response.StatusCode;
        _collision = await response.ReadJson<HttpProblemDetails>();
        _secondUser = await Host.WaitForUserAsync(
            userId,
            token,
            u => u.Status == "in_progress");
    }

    [Test]
    public async Task Then_the_collision_is_rejected_without_side_effects()
    {
        await Assert.That(_firstAliasStatus).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(_collisionStatus).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        await Assert.That(_collision.Title).IsEqualTo("alias_already_claimed");
        await Assert.That(string.IsNullOrEmpty(_secondUser.Alias)).IsTrue();
    }
}
