using System.Security.Claims;
using Funzo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tooyioo.Common;
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Tooyioo.Api.Infrastructure.Auth;

public static class AuthExtensions
{
    private const string DefaultScheme = "default-scheme";

    public static TBuilder AddAuth<TBuilder>(this TBuilder builder) 
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddOptions<GoogleJwtOptions>()
            .BindConfiguration(GoogleJwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddOptions<UserLoginOptions>()
            .BindConfiguration(UserLoginOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(o => IsValidPepper(o.Pepper),
                "The pepper must be a base64 string of at least 32 bytes.")
            .ValidateOnStart();

        builder.Services
            .AddSingleton<IUserLoginKeyFactory, HmacUserLoginKeyFactory>()
            .AddSingleton<IUserLoginResolver, UserLoginResolver>()
            .AddScoped<IExternalIdentityRetriever, ExternalIdentityRetriever>();

        builder.Services
            .AddAuthentication(DefaultScheme)
            .AddJwtBearer(DefaultScheme);

        builder.Services
            .AddOptions<JwtBearerOptions>(DefaultScheme)
            .Configure<IOptions<GoogleJwtOptions>>((options, googleJwtOptions) =>
            {
                ConfigureGoogleJwt(options, googleJwtOptions.Value);
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = OnTokenValidated,
                    OnChallenge = OnChallenge,
                    OnForbidden = OnForbidden
                };
            });

        var userRequiredPolicy = new AuthorizationPolicyBuilder(DefaultScheme)
            .RequireAuthenticatedUser()
            .RequireClaim(Claims.UserId)
            .Build();

        builder.Services
            .AddAuthorizationBuilder()
            .SetDefaultPolicy(userRequiredPolicy)
            .SetFallbackPolicy(userRequiredPolicy)
            .AddPolicy(AuthorizationPolicies.SubRequired, policy => policy
                .AddAuthenticationSchemes(DefaultScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(Claims.Sub)
                .RequireClaim(Claims.Issuer))
            .AddPolicy(AuthorizationPolicies.UserRequired, userRequiredPolicy);

        return builder;
    }

    private static bool IsValidPepper(string pepper)
    {
        if (string.IsNullOrWhiteSpace(pepper))
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(pepper).Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void ConfigureGoogleJwt(JwtBearerOptions options, GoogleJwtOptions googleJwtOptions)
    {
        // JWT validation is the issuer security boundary: the raw iss must match exactly.
        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
        options.Authority = googleJwtOptions.Issuer;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = [googleJwtOptions.Issuer],

            ValidateAudience = true,
            ValidAudiences = [googleJwtOptions.ClientId],

            ValidateLifetime = true,
            NameClaimType = "sub"
        };
    }

    private static async Task OnTokenValidated(TokenValidatedContext context)
    {
        var claimsPrincipal = context.Principal;

        if (claimsPrincipal?.Identity is not ClaimsIdentity claimsIdentity)
        {
            context.Fail("JWT validation did not produce a claims identity");
            return;
        }

        ExternalIdentity externalIdentity;
        try
        {
            externalIdentity = ExternalIdentityClaimsParser.Parser(claimsPrincipal);
        }
        catch (Exception ex)
        {
            context.Fail(ex.Message);
            return;
        }

        var userLoginResolver = context.HttpContext.RequestServices.GetRequiredService<IUserLoginResolver>();

        Option<UserId> resolution;
        try
        {
            resolution = await userLoginResolver.Resolve(externalIdentity, context.HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            context.Fail(ex.Message);
            return;
        }

        if (resolution.IsSome(out var userId))
        {
            claimsIdentity.AddClaim(new Claim(Claims.UserId, userId.Value));
        }
    }

    private static async Task OnForbidden(ForbiddenContext context)
    {
        var http = context.HttpContext;
        http.Response.StatusCode = StatusCodes.Status403Forbidden;

        await WriteProblemAsync(http,
            new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden, Title = "forbidden",
                Detail = "You are not allowed to access this resource."
            });
    }

    private static async Task OnChallenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse(); // suppress default 401

        var http = context.HttpContext;
        http.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await WriteProblemAsync(http,
            new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized, Title = "unauthorized",
                Detail = "Missing or invalid bearer token."
            });
    }

    private static ValueTask WriteProblemAsync(HttpContext http, ProblemDetails problemDetails)
    {
        var service = http.RequestServices.GetRequiredService<IProblemDetailsService>();

        return service.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = problemDetails
        });
    }
}
