using System.Reflection;
using Eventuous;
using Eventuous.KurrentDB;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Commands;
using Slicent.Application.Queries;
using Slicent.EventStore;
// ReSharper disable UnusedType.Global
// ReSharper disable ConvertToExtensionBlock
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Slicent;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSlicent(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.TryAddTransient(typeof(CommandInvoker<,>), typeof(CommandInvoker<,>));
        services.TryAddScoped<ICommandDispatcher, CommandDispatcher>();
        
        services.TryAddTransient(typeof(QueryInvoker<,>), typeof(QueryInvoker<,>));
        services.TryAddScoped<IQueryDispatcher, QueryDispatcher>();
        services.TryAddSingleton(typeof(QueryService<,>));
        
        var candidates = GetConcreteTypes(assemblies);
        
        AddCommandHandlersAndInvokers(services, candidates);
        AddQueryHandlersAndInvokers(services, candidates);
        
        services.TryAddScoped(typeof(CommandHttpResponseGenerator<>));
        services.TryAddScoped(typeof(QueryHttpResponseGenerator<>));
        
        AddClosedGenericImplementations(services, candidates, typeof(ICommandMapper<,,>), ServiceLifetime.Scoped);
        AddClosedGenericImplementations(services, candidates, typeof(ICommandMapper<,,,>), ServiceLifetime.Scoped);
        AddClosedGenericImplementations(services, candidates, typeof(ICommandHttpResponseMapper<,,>), ServiceLifetime.Scoped);
        
        AddClosedGenericImplementations(services, candidates, typeof(IQueryMapper<,>), ServiceLifetime.Scoped);
        AddClosedGenericImplementations(services, candidates, typeof(IQueryMapper<,,>), ServiceLifetime.Scoped);
        AddClosedGenericImplementations(services, candidates, typeof(IQueryHttpResponseMapper<,,>), ServiceLifetime.Scoped);
        
        AddClosedGenericImplementations(services, candidates, typeof(IRouteAuthorizer<>), ServiceLifetime.Scoped, true);
        AddClosedGenericImplementations(services, candidates, typeof(IPayloadAuthorizer<>), ServiceLifetime.Scoped, true);
        AddClosedGenericImplementations(services, candidates, typeof(IRoutePayloadAuthorizer<,>), ServiceLifetime.Scoped, true);
        
        AddAssignableImplementations(services, candidates, typeof(IHttpEndpointModule), ServiceLifetime.Singleton);
        
        services.AddCustomProblemDetails();
        
        var typeMapper = EventTypeMapperFactory.Create([.. assemblies.Distinct()]);
        services.AddSingleton<ITypeMapper>(typeMapper);
        services.AddSingleton<IEventSerializer, JsonEventSerializer>();

        return services;
    }

    public static IServiceCollection AddSlicentKurrentDb(this IServiceCollection services, string connectionString)
    {
        services.AddKurrentDBClient(connectionString);
        services.AddEventStore<KurrentDBEventStore>();
        
        return services;
    }
    
    public static IServiceCollection AddSlicentMongoDb(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionStringFactory,
        Func<IServiceProvider, string> databaseNameFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionStringFactory);
        ArgumentNullException.ThrowIfNull(databaseNameFactory);

        services.AddSingleton(sp =>
        {
            var mongoClientSettings =
                MongoClientSettings.FromConnectionString(connectionStringFactory(sp));
            return new MongoClient(mongoClientSettings);
        });

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<MongoClient>();
            return client.GetDatabase(databaseNameFactory(sp));
        });

        return services;
    }

    private static void AddCommandHandlersAndInvokers(IServiceCollection services, Type[] candidates)
    {
        foreach (var implType in candidates)
        {
            var handlerInterfaces = implType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
                .ToArray();

            if (handlerInterfaces.Length == 0)
            {
                continue;
            }

            foreach (var handlerInterface in handlerInterfaces)
            {
                services.TryAdd(new ServiceDescriptor(handlerInterface, implType, ServiceLifetime.Scoped));
            }

            foreach (var handlerInterface in handlerInterfaces)
            {
                var args = handlerInterface.GetGenericArguments();
                var commandType = args[0];
                var resultType = args[1];

                var invokerService = typeof(ICommandInvoker<>).MakeGenericType(resultType);
                var invokerImpl = typeof(CommandInvoker<,>).MakeGenericType(commandType, resultType);

                services.TryAdd(new ServiceDescriptor(invokerService, invokerImpl, ServiceLifetime.Scoped));
            }
        }
    }
    
    private static void AddQueryHandlersAndInvokers(IServiceCollection services, Type[] candidates)
    {
        foreach (var implType in candidates)
        {
            var handlerInterfaces = implType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                .ToArray();

            if (handlerInterfaces.Length == 0)
            {
                continue;
            }

            foreach (var handlerInterface in handlerInterfaces)
            {
                services.TryAdd(new ServiceDescriptor(handlerInterface, implType, ServiceLifetime.Scoped));
            }

            foreach (var handlerInterface in handlerInterfaces)
            {
                var args = handlerInterface.GetGenericArguments();
                var queryType = args[0];
                var resultType = args[1];

                var invokerService = typeof(IQueryInvoker<>).MakeGenericType(resultType);
                var invokerImpl = typeof(QueryInvoker<,>).MakeGenericType(queryType, resultType);

                services.TryAdd(new ServiceDescriptor(invokerService, invokerImpl, ServiceLifetime.Scoped));
            }
        }
    }
    
    private static Type[] GetConcreteTypes(params Assembly[] assemblies)
        => assemblies
            .Distinct()
            .SelectMany(a => a.DefinedTypes)
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Select(t => t.AsType())
            .ToArray();

    private static void AddCustomProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                if (ctx.ProblemDetails is HttpValidationProblemDetails httpValidationProblemDetails)
                {
                    ctx.ProblemDetails = HttpProblemDetails.FromValidation(ctx.HttpContext, httpValidationProblemDetails);
                    return;
                }

                ctx.ProblemDetails = ctx.ProblemDetails.Status switch
                {
                    StatusCodes.Status401Unauthorized => HttpProblemDetails.FromUnauthorized(ctx.HttpContext),
                    StatusCodes.Status403Forbidden => HttpProblemDetails.FromForbidden(ctx.HttpContext),
                    _ => ctx.ProblemDetails
                };
            };
        });
    }
    
    private static void AddClosedGenericImplementations(
        IServiceCollection services,
        Type[] candidates,
        Type openGenericService,
        ServiceLifetime lifetime,
        bool includeStandaloneRegistration = false)
    {
        foreach (var implType in candidates)
        {
            foreach (var serviceType in implType.GetInterfaces()
                         .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericService))
            {
                services.TryAdd(new ServiceDescriptor(serviceType, implType, lifetime));
                if (includeStandaloneRegistration)
                {
                    services.TryAdd(new ServiceDescriptor(implType, implType, lifetime));
                }
            }
        }
    }
    
    private static void AddAssignableImplementations(
        IServiceCollection services,
        Type[] candidates,
        Type serviceType,
        ServiceLifetime lifetime)
    {
        foreach (var implType in candidates)
        {
            if (!serviceType.IsAssignableFrom(implType))
            {
                continue;
            }

            services.TryAddEnumerable(new ServiceDescriptor(serviceType, implType, lifetime));
        }
    }
}
