using System.Reflection;
using Tooyioo.Api.Infrastructure.Auth;
using Tooyioo.Api.Infrastructure.EventStoreStateRetriever;
using Tooyioo.Api.Infrastructure.Health;
using Tooyioo.Api.Infrastructure.OpenApi;
using Tooyioo.Api.Infrastructure.Slicent;
using Tooyioo.Api.Infrastructure.Telemetry;
using Tooyioo.Api.Infrastructure.Validation;

var builder = WebApplication.CreateBuilder(args);

// Build-time OpenAPI generation runs with the Development environment so it sees
// appsettings.Development.json, but the generation host intentionally skips runtime
// infrastructure. Do not let Development's DI validation fail on those skipped services.
if (Assembly.GetEntryAssembly().IsOpenApiGenerationLaunch())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateOnBuild = false;
        options.ValidateScopes = false;
    });
}

var sliceAssemblies =
    new[]
    {
        typeof(Tooyioo.User.SliceAssemblyMarker).Assembly,
        typeof(Tooyioo.Business.SliceAssemblyMarker).Assembly
    };

var domainEventAssemblies =
    new[]
    {
        typeof(Tooyioo.User.Contracts.ContractsAssemblyMarker).Assembly,
        typeof(Tooyioo.Business.Contracts.ContractsAssemblyMarker).Assembly
    };

var allAssemblies = sliceAssemblies
    .Concat(domainEventAssemblies)
    .Distinct()
    .ToArray();

builder
    .AddHealth()
    .AddTelemetry()
    .AddOpenApi()
    //.AddPhoneNumberValidator()
    .AddEventStoreDetailsRetrievers()
    .AddSlicent(allAssemblies)
    .AddAuth()
    .AddMinimalApiValidations(allAssemblies);

var app = builder.Build();

app.Logger.LogInformation("Application is starting..");

app
    .UseOpenApi()
    .UseSlicent()
    .UseHealth();

app.Run();

#pragma warning disable ASP0027
// ReSharper disable once ClassNeverInstantiated.Global
// Partial class created for WebApplicationFactory to use the real Program class in tests and override on top of it
namespace Tooyioo.Api
{
    // ReSharper disable once PartialTypeWithSinglePart
    public partial class Program;
}
#pragma warning restore ASP0027
