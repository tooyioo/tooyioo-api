using Tooyioo.Api.Infrastructure.Auth;
using Tooyioo.Api.Infrastructure.EventStoreStateRetriever;
using Tooyioo.Api.Infrastructure.Health;
using Tooyioo.Api.Infrastructure.MinimalApiValidator;
using Tooyioo.Api.Infrastructure.OpenApi;
using Tooyioo.Api.Infrastructure.Slicent;
using Tooyioo.Api.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

var sliceAssemblies =
    new[]
    {
        typeof(Tooyioo.User.SliceAssemblyMarker).Assembly,
        typeof(Tooyioo.UserOnboarding.SliceAssemblyMarker).Assembly,
        typeof(Tooyioo.BusinessOnboarding.SliceAssemblyMarker).Assembly
    };

var domainEventAssemblies =
    new[]
    {
        typeof(Tooyioo.UserOnboarding.Contracts.ContractsAssemblyMarker).Assembly,
        typeof(Tooyioo.BusinessOnboarding.Contracts.ContractsAssemblyMarker).Assembly
    };

var allAssemblies = sliceAssemblies.Concat(domainEventAssemblies).ToArray();

#pragma warning disable ASP0029
builder
    .AddHealth()
    .AddTelemetry()
    .AddOpenApi()
    //.AddPhoneNumberValidator()
    .AddEventStoreDetailsRetrievers()
    .AddMinimalApiValidator(sliceAssemblies)
    .AddSlicent(allAssemblies)
    .AddAuth();

#pragma warning restore ASP0029

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
