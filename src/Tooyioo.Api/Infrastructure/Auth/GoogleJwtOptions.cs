using System.ComponentModel.DataAnnotations;

namespace Tooyioo.Api.Infrastructure.Auth;

public sealed class GoogleJwtOptions
{
    public const string SectionName = "Google";

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;
}
