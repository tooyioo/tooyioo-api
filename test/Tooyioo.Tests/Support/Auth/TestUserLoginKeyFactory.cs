using Tooyioo.Common;

namespace Tooyioo.Tests.Support.Auth;

/// <summary>
/// Test seam: uses the subject itself as the user login key so tests can seed
/// and assert credential claim streams without depending on the HMAC pepper.
/// </summary>
internal sealed class TestUserLoginKeyFactory
    : IUserLoginKeyFactory
{
    public UserLoginKey ForOidc(string issuer, string subject)
        => new(subject, "oidc", "https://accounts.google.com");
}
