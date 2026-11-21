using System.Net;
using Tooyioo.Tests.Support.Http;
using Tooyioo.User.Features.InitiateUserOnboarding.Contracts;

namespace Tooyioo.Tests.Support.Smoke;

/// <summary>
/// Given/When/Then base for smoke scenarios. The expensive host is shared across the
/// assembly; each scenario gets its own <see cref="SmokeTenant"/> so data does not collide.
/// </summary>
[NotInParallel]
[ClassDataSource<SmokeTestHost>(Shared = SharedType.PerAssembly)]
public abstract class SmokeScenario(SmokeTestHost host)
{
    protected SmokeTestHost Host { get; } = host;

    protected SmokeTenant Tenant { get; } = SmokeTenant.New();

    [Before(Test)]
    public async Task ArrangeAndAct()
    {
        await Given();
        await When();
    }

    protected virtual Task Given()
        => Task.CompletedTask;

    protected abstract Task When();

    protected async Task<string> CreateUserAsync(string token)
    {
        using var response = await Host.HttpClient.PostJson("/users", new { }, token);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var body = await response.ReadJson<InitiateUserOnboardingResponse>();
        return body.UserId;
    }
}
