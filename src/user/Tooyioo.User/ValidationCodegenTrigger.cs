using Microsoft.Extensions.DependencyInjection;

namespace Tooyioo.User;

/// <summary>
/// Compile-time hook for the validation source generator, which only emits metadata for
/// assemblies that contain an <c>AddValidation</c> call. This method is intentionally never
/// invoked at runtime: the host discovers the generated resolver and registers it.
/// See https://github.com/dotnet/AspNetCore.Docs/issues/35090#issuecomment-3661156506.
/// </summary>
internal static class ValidationCodegenTrigger
{
    public static IServiceCollection Trigger(this IServiceCollection services)
        => services.AddValidation();
}
