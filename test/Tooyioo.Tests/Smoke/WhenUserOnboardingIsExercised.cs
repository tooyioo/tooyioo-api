using System.Net;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.Smoke;
using Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;
using Tooyioo.User.Features.CompleteUserOnboarding.Contracts;
using Tooyioo.User.Features.RetrieveUser.Contract;

namespace Tooyioo.Tests.Smoke;

/// <summary>
/// The full user onboarding journey: command -> KurrentDB -> subscription -> projection ->
/// Mongo read model -> query. Business rules stay in the Features tests.
/// </summary>
[Category(TestCategories.Smoke)]
public sealed class WhenUserOnboardingIsExercised(SmokeTestHost host)
    : SmokeScenario(host)
{
    private string _alias = null!;
    private string _email = null!;
    private HttpStatusCode _aliasStatus;
    private HttpStatusCode _completionStatus;
    private RetrieveUserResponse _user = null!;

    protected override async Task When()
    {
        var token = Host.Token(Tenant.Subject("user"), "Jane", "Bloggs", Tenant.Email("user"));
        _alias = Tenant.Alias("user");
        _email = Tenant.Email("user");

        var userId = await CreateUserAsync(token);

        using var aliasResponse = await Host.HttpClient.PutJson(
            $"/users/{userId}/onboarding/alias",
            new ChooseUserOnboardingAliasRequest { Alias = _alias },
            token);
        _aliasStatus = aliasResponse.StatusCode;

        using var completionResponse = await Host.HttpClient.PutJson(
            $"/users/{userId}/onboarding/completion",
            new CompleteUserOnboardingRequest
            {
                TermsOfServiceVersion = "1.0.0",
                PrivacyPolicyVersion = "1.0.0"
            },
            token);
        _completionStatus = completionResponse.StatusCode;

        _user = await Host.WaitForUserAsync(
            userId,
            token,
            u => u.Status == "completed" && u.Alias == _alias);
    }

    [Test]
    public async Task Then_the_whole_flow_is_projected_into_the_read_model()
    {
        await Assert.That(_aliasStatus).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(_completionStatus).IsEqualTo(HttpStatusCode.OK);

        await Assert.That(_user.Name).IsEqualTo("Jane");
        await Assert.That(_user.LastName).IsEqualTo("Bloggs");
        await Assert.That(_user.Email).IsEqualTo(_email);
        await Assert.That(_user.Alias).IsEqualTo(_alias);
        await Assert.That(_user.Status).IsEqualTo("completed");
        await Assert.That(_user.TermsOfServiceVersion).IsEqualTo("1.0.0");
        await Assert.That(_user.PrivacyPolicyVersion).IsEqualTo("1.0.0");
    }
}
