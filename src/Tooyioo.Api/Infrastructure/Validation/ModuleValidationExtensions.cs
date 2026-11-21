using System.Reflection;
using Microsoft.Extensions.Validation;

namespace Tooyioo.Api.Infrastructure.Validation;

/// <summary>
/// Registers the source-generated validation resolvers of the given module assemblies, so a
/// module only has to expose its assembly instead of a per-module AddValidation extension.
/// </summary>
public static class ModuleValidationExtensions
{
    public static TBuilder AddMinimalApiValidations<TBuilder>(
        this TBuilder builder,
        params Assembly[] assemblies)
        where TBuilder : IHostApplicationBuilder
    {
        var resolvers = assemblies
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && typeof(IValidatableInfoResolver).IsAssignableFrom(type))
            .Select(type => (IValidatableInfoResolver)Activator.CreateInstance(type)!)
            .ToArray();

        builder.Services.AddValidation(options =>
        {
            foreach (var resolver in resolvers)
            {
                options.Resolvers.Add(resolver);
            }
        });

        return builder;
    }
}
