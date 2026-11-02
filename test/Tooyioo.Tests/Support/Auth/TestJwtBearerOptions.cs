using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Tooyioo.Common;

namespace Tooyioo.Tests.Support.Auth;

internal sealed class TestJwtBearerOptions(byte[] signingKey)
    : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        options.RequireHttpsMetadata = false;
        options.Authority = null!;
        options.MetadataAddress = null!;
        options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
            new OpenIdConnectConfiguration());

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Mirror the production issuer boundary: the parser reads raw iss/sub,
            // so unsupported issuers must be rejected by JWT validation, not by the parser.
            ValidateIssuer = true,
            ValidIssuers = [GoogleIdentityTokenBuilder.Issuer],
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKey),
            NameClaimType = Claims.Sub
        };
    }
}
