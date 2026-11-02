using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tooyioo.Common;
// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.Api.Infrastructure.Auth;

internal sealed class HmacUserLoginKeyFactory
    : IUserLoginKeyFactory
{
    private readonly byte[] _pepper;

    public HmacUserLoginKeyFactory(IOptions<UserLoginOptions> options)
    {
        _pepper = Convert.FromBase64String(options.Value.Pepper);
    }

    public UserLoginKey ForOidc(string issuer, string subject)
    {
        var message = Encoding.UTF8.GetBytes($"oidc|{issuer}|{subject}");
        var mac = HMACSHA256.HashData(_pepper, message);
        var key = Base64UrlEncoder.Encode(mac);

        return new UserLoginKey(key, "oidc", issuer);
    }
}
