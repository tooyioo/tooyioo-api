using System.ComponentModel.DataAnnotations;
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.Api.Infrastructure.Auth;

public sealed class UserLoginOptions
{
    public const string SectionName = "UserLogin";

    [Required]
    public string Pepper { get; init; } = string.Empty;
}
