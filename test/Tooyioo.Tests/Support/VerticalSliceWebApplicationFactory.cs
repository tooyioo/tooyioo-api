using Eventuous;
using Eventuous.Testing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Tooyioo.Common;
using Tooyioo.Tests.Support.Auth;

namespace Tooyioo.Tests.Support;

internal sealed class VerticalSliceWebApplicationFactory(
    InMemoryEventStore eventStore,
    byte[] signingKey,
    MongoDatabaseTestOptions? mongoDatabase,
    Action<IServiceCollection>? overrideServices)
    : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Slicent:EventStoreProvider"] = "InMemory",
                ["ConnectionStrings:MongoDb"] = mongoDatabase?.ConnectionString ?? "mongodb://127.0.0.1:27017"
            });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:ClientId"] = "tooyioo-tests"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();

            services.RemoveAll<IEventStore>();
            services.RemoveAll<IEventReader>();
            services.RemoveAll<IEventWriter>();
            services.AddSingleton<IEventStore>(eventStore);
            services.AddSingleton<IEventReader>(eventStore);
            services.AddSingleton<IEventWriter>(eventStore);

            services.RemoveAll<IMongoDatabase>();
            services.RemoveAll<MongoClient>();

            var mongoClient = new MongoClient(mongoDatabase?.ConnectionString ?? "mongodb://127.0.0.1:27017");
            services.AddSingleton(mongoClient);
            services.AddSingleton<IMongoDatabase>(_ =>
                mongoClient.GetDatabase(mongoDatabase?.DatabaseName ?? "TooyiooVerticalSliceTests"));

            services.RemoveAll<IUserLoginKeyFactory>();
            services.AddSingleton<IUserLoginKeyFactory, TestUserLoginKeyFactory>();

            services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>>(new TestJwtBearerOptions(signingKey));

            overrideServices?.Invoke(services);
        });
    }
}

internal sealed record MongoDatabaseTestOptions(string ConnectionString, string DatabaseName);
