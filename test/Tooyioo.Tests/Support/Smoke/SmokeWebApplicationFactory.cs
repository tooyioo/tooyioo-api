using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tooyioo.Tests.Support.Auth;
using Tooyioo.User.Identity;

namespace Tooyioo.Tests.Support.Smoke;

/// <summary>
/// Hosts the API with the real KurrentDB and MongoDB providers, keeping the projection
/// subscription running. Unlike the vertical slice factory, hosted services are not removed.
/// </summary>
internal sealed class SmokeWebApplicationFactory(
    byte[] signingKey,
    string mongoDbConnectionString)
    : WebApplicationFactory<Program>
{
    private const string TestPepper = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Slicent:EventStoreProvider"] = "KurrentDb"
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
                ["MongoDb:ConnectionString"] = mongoDbConnectionString,
                ["UserLogin:Pepper"] = TestPepper
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserLoginKeyFactory>();
            services.AddSingleton<IUserLoginKeyFactory, TestUserLoginKeyFactory>();
            services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>>(
                new TestJwtBearerOptions(signingKey));
        });
    }
}
