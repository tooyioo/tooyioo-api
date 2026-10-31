using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Tooyioo.Tests.Support;

[Category(TestCategories.Infrastructure)]
public sealed class VerticalSliceTestHostInfrastructureTests
{
    [Test]
    public async Task Does_not_register_the_production_kurrentdb_client()
    {
        await using var host = await VerticalSliceTestHost.Start();

        await Assert.That(host.Services.GetService<KurrentDBClient>()).IsNull();
    }
}
