using System.Reflection;
using Eventuous.KurrentDB.Subscriptions;
using Eventuous.Projections.MongoDB;
using Eventuous.Subscriptions.Registrations;
using KurrentDB.Client;
using Slicent;
using Slicent.Application;
using Tooyioo.Api.Infrastructure.OpenApi;
using Tooyioo.BusinessOnboarding.Features.Support;
using Tooyioo.User.Features.Support;
using Tooyioo.UserOnboarding.Features.Support;

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
        var mongoDbOptions = builder.Configuration
            .GetSection(MongoDbOptions.SectionName)
            .Get<MongoDbOptions>()
            ?? new MongoDbOptions();

        builder.Services.AddSlicentMongoDb(mongoDbOptions.MongoDb);

        if (slicentOptions.EventStoreProvider is EventStoreProvider.InMemory)
        {
            return builder;
        }

        builder.Services
            .AddOptions<KurrentDbOptions>()
            .BindConfiguration(KurrentDbOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var kurrentDbOptions = builder.Configuration
            .GetSection(KurrentDbOptions.SectionName)
            .Get<KurrentDbOptions>()
            ?? new KurrentDbOptions();

        builder.Services
            .AddSlicentKurrentDb(kurrentDbOptions.KurrentDb)
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
                    .AddEventHandler<UserOnboardingProjector>()
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
