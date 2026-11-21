using System.Reflection;
using Eventuous.KurrentDB.Subscriptions;
using Eventuous.Projections.MongoDB;
using Eventuous.Subscriptions.Registrations;
using KurrentDB.Client;
using Microsoft.Extensions.Options;
using Slicent;
using Slicent.Application;
using Tooyioo.Api.Infrastructure.OpenApi;
using Tooyioo.Api.Infrastructure.UserPersonalData;
using Tooyioo.Business.Features.Support;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Api.Infrastructure.Slicent;

public static class SlicentExtensions
{
    public static TBuilder AddSlicent<TBuilder>(this TBuilder builder, params Assembly[] assemblies) 
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSlicent(assemblies);

        if (Assembly.GetEntryAssembly().IsOpenApiGenerationLaunch())
        {
            return builder;
        }

        builder.Services
            .AddOptions<SlicentOptions>()
            .BindConfiguration(SlicentOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddOptions<MongoDbOptions>()
            .BindConfiguration(MongoDbOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var slicentOptions = builder.Configuration
            .GetSection(SlicentOptions.SectionName)
            .Get<SlicentOptions>()
            ?? new SlicentOptions();

        builder.Services.AddSlicentMongoDb(
            sp => sp.GetRequiredService<IOptions<MongoDbOptions>>().Value.ConnectionString,
            sp => sp.GetRequiredService<IOptions<MongoDbOptions>>().Value.Database);

        builder.Services
            .AddOptions<UserPersonalDataErasureOptions>()
            .BindConfiguration(UserPersonalDataErasureOptions.SectionName);

        builder.Services.AddHostedService<UserPersonalDataErasureScheduler>();

        if (slicentOptions.EventStoreProvider is EventStoreProvider.InMemory)
        {
            return builder;
        }

        builder.Services
            .AddOptions<KurrentDbOptions>()
            .BindConfiguration(KurrentDbOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // AddKurrentDBClient takes the connection string at registration time and adds its
        // instrumentation, so this read remains eager. Smoke tests override it through the
        // environment until the client exposes a factory-friendly registration overload.
        var kurrentDbOptions = builder.Configuration
            .GetSection(KurrentDbOptions.SectionName)
            .Get<KurrentDbOptions>()
            ?? new KurrentDbOptions();

        if (string.IsNullOrWhiteSpace(kurrentDbOptions.ConnectionString))
        {
            throw new InvalidOperationException(
                $"{KurrentDbOptions.SectionName}:{nameof(KurrentDbOptions.ConnectionString)} is required.");
        }

        builder.Services
            .AddSlicentKurrentDb(kurrentDbOptions.ConnectionString)
            .AddSubscription<AllStreamSubscription, AllStreamSubscriptionOptions>(
                "ReadModelsSubscription",
                subscriptionBuilder => subscriptionBuilder
                    .Configure(configureOptions =>
                    {
                        configureOptions.ThrowOnError = true;
                        configureOptions.EventFilter = EventTypeFilter.ExcludeSystemEvents();
                        configureOptions.CheckpointInterval = 10;
                    })
                    .UseCheckpointStore<MongoCheckpointStore>()
                    .AddEventHandler<UserPersonalDataProjector>()
                    .AddEventHandler<UserProjector>()
                    .AddEventHandler<BusinessOnboardingProjector>());
            
        return builder;
    }

    public static WebApplication UseSlicent(this WebApplication app)
    {
        foreach (var module in app.Services.GetServices<IHttpEndpointModule>())
        {
            module.Map(app);
        }
        
        return app;
    }
}
