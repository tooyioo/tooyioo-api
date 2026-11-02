namespace Tooyioo.Common;

/// <summary>
/// A pseudonymous user login key derived from a provider identity.
/// </summary>
/// <param name="Key">The Base64Url-encoded HMAC of the credential.</param>
/// <param name="Kind">The credential kind, for example <c>oidc</c>.</param>
/// <param name="Issuer">The issuer that identifies the provider.</param>
public sealed record UserLoginKey(
    string Key,
    string Kind,
    string Issuer);
