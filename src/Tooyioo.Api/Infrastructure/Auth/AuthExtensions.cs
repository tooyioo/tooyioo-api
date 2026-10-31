// ReSharper disable UnusedType.Global
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tooyioo.Common;

namespace Tooyioo.Api.Infrastructure.Auth;

public static class AuthExtensions
{
    private const string DefaultScheme = "default-scheme";

    public static TBuilder AddAuth<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddOptions<GoogleJwtOptions>()
            .BindConfiguration(GoogleJwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

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

        builder.Services
            .AddSingleton<IExternalIdentityStatusResolver, ExternalIdentityStatusResolver>()
            .AddScoped<IExternalIdentityRetriever, ExternalIdentityRetriever>();

        builder.Services
            .AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder(DefaultScheme)
                .RequireAuthenticatedUser()
                .Build())
            .SetFallbackPolicy(new AuthorizationPolicyBuilder(DefaultScheme)
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(AuthorizationPolicies.ExternalIdentityRequired, policy => policy
                .AddAuthenticationSchemes(DefaultScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(Claims.Sub)
                .RequireClaim(Claims.Issuer))
            .AddPolicy(AuthorizationPolicies.UserOnboardingRequired, policy => policy
                .AddAuthenticationSchemes(DefaultScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(Claims.UserOnboardingId))
            .AddPolicy(AuthorizationPolicies.UserRequired, policy => policy
                .AddAuthenticationSchemes(DefaultScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(Claims.UserId));

        return builder;
    }

    private static void ConfigureGoogleJwt(JwtBearerOptions options, GoogleJwtOptions googleJwtOptions)
    {
        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
        options.Authority = "https://accounts.google.com";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
            
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
        
        var resolver = context.HttpContext.RequestServices.GetRequiredService<IExternalIdentityStatusResolver>();
        var resolutionStatusResult = await resolver.Resolve(externalIdentity, context.HttpContext.RequestAborted);

        context.Principal = resolutionStatusResult.Match(
            userOnboarding => AddClaims(
                claimsPrincipal,
                claimsIdentity, new Claim( Claims.UserOnboardingId,userOnboarding.UserOnboardingId)),

            userOnboarded => AddClaims(
                claimsPrincipal,
                claimsIdentity, 
                new Claim(Claims.UserOnboardingId, userOnboarded.UserOnboardingId), 
                new Claim(Claims.UserId, userOnboarded.UserId)),

            _ => claimsPrincipal);
    }

    private static ClaimsPrincipal AddClaims(
        ClaimsPrincipal principal,
        ClaimsIdentity identity,
        params Claim[] claims)
    {
        identity.AddClaims(claims);
        return principal;
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
