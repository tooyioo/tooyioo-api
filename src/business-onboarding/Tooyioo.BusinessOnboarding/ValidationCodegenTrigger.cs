using Microsoft.Extensions.DependencyInjection;

namespace Tooyioo.BusinessOnboarding;

/// <summary>
/// This is required for validation source generation to find an .AddValidation() within the assembly.
/// See https://github.com/dotnet/AspNetCore.Docs/issues/35090#issuecomment-3661156506
/// </summary>
internal static class ValidationCodegenTrigger
{
    public static IServiceCollection Trigger(this IServiceCollection services) 
        => services.AddValidation();
}