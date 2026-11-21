using System.Reflection;
using Microsoft.Extensions.Validation;

namespace Tooyioo.Api.Infrastructure.MinimalApiValidator;

public static class MinimalApiValidatorExtensions
{
    public static TBuilder AddMinimalApiValidator<TBuilder>(this TBuilder builder, params Assembly[] assemblies) 
        where TBuilder : IHostApplicationBuilder
    {
        var types = assemblies.SelectMany(a => a.GetTypes()).ToList();
        
        // Scan for all validatable DTO after source generation happened.
        var validatableInfoResolvers = types
            .Where(t => t.GetInterfaces().Any(i => i.IsAssignableTo(typeof(IValidatableInfoResolver))))
            .Select(Activator.CreateInstance).Cast<IValidatableInfoResolver>()
            .ToList();

        builder.Services
            .AddValidation(options =>
            {
                foreach (var validatableInfoResolver in validatableInfoResolvers)
                {
                    options.Resolvers.Add(validatableInfoResolver);
                }
            });
        
        return builder;
    }
}