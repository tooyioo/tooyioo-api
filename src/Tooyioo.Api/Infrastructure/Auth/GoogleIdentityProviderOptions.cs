using System.ComponentModel.DataAnnotations;

namespace Tooyioo.Api.Infrastructure.Auth;

/// <summary>
/// Google as the external identity provider. The name makes it explicit that these settings
/// describe the OpenID Connect provider used to validate the id_token, not a generic Google
/// integration.
/// </summary>
public sealed class GoogleIdentityProviderOptions
{
    public const string SectionName = "GoogleIdentityProvider";

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;
}
