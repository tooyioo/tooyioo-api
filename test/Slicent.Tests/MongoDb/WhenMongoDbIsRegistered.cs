using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Slicent;

namespace Slicent.Tests.MongoDb;

public sealed class WhenMongoDbIsRegistered
{
    [Test]
    public async Task Then_database_name_is_configurable()
    {
        var services = new ServiceCollection();

        services.AddSlicentMongoDb(
            _ => "mongodb://localhost:27017",
            _ => "smoke_database");

        await using var provider = services.BuildServiceProvider();
        var database = provider.GetRequiredService<IMongoDatabase>();

        await Assert.That(database.DatabaseNamespace.DatabaseName).IsEqualTo("smoke_database");
    }
}
