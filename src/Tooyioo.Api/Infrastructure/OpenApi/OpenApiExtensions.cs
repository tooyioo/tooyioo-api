using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Tooyioo.Api.Infrastructure.OpenApi;

public static class OpenApiExtensions
{
    private const string DocumentName = "public";
    private const string GoogleOAuthSecurityScheme = "GoogleOAuth";

    public static TBuilder AddOpenApi<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddOptions<ScalarGoogleOAuthOptions>()
            .BindConfiguration(ScalarGoogleOAuthOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.AddOpenApi(DocumentName, options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            
            options.AddDocumentTransformer((document, context, _) =>
            {
                var googleOAuthOptions = context.ApplicationServices
                    .GetRequiredService<IOptions<ScalarGoogleOAuthOptions>>()
                    .Value;

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                
                document.Components.SecuritySchemes[GoogleOAuthSecurityScheme] =
                    new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri(googleOAuthOptions.AuthorizationUrl),
                                TokenUrl = new Uri(googleOAuthOptions.TokenUrl),
                                Scopes = new Dictionary<string, string>
                                {
                                    ["openid"] = "OpenID Connect",
                                    ["email"] = "Email",
                                    ["profile"] = "Profile"
                                }
                            }
                        }
                    };

                return Task.CompletedTask;
            });
                
            options.AddOperationTransformer((operation, context, _) =>
            {
                operation.Security ??= new List<OpenApiSecurityRequirement>();

                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [ new OpenApiSecuritySchemeReference(GoogleOAuthSecurityScheme, context.Document) ] = 
                    [
                        "openid",
                        "email",
                        "profile"
                    ]
                });

                AddCreatedLocationHeader(operation);

                return Task.CompletedTask;
            });
        });

        return builder;
    }

    private static void AddCreatedLocationHeader(OpenApiOperation operation)
    {
        if (operation.Responses is null
            || !operation.Responses.TryGetValue("201", out var created)
            || created is not OpenApiResponse response)
        {
            return;
        }

        response.Headers ??= new Dictionary<string, IOpenApiHeader>();
        response.Headers["Location"] = new OpenApiHeader
        {
            Description = "URI of the created resource.",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "uri"
            }
        };
    }

    public static WebApplication UseOpenApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        var googleOAuthOptions = app.Services
            .GetRequiredService<IOptions<ScalarGoogleOAuthOptions>>()
            .Value;
        var googleOAuthClientSecret = googleOAuthOptions.ClientSecret
            ?? throw new InvalidOperationException($"{ScalarGoogleOAuthOptions.SectionName} is missing {nameof(googleOAuthOptions.ClientSecret)}");
        
        app.MapOpenApi().AllowAnonymous().CacheOutput();

        app.MapScalarApiReference(
                "/scalar",
                options =>
                    options
                        .ShowOperationId()
                        .SortTagsAlphabetically()
                        .WithOperationTitleSource(OperationTitleSource.Path)
                        .WithOpenApiRoutePattern($"/openapi/{DocumentName}.json")
                        .WithDocumentDownloadType(DocumentDownloadType.Json)
                        .AddPreferredSecuritySchemes(GoogleOAuthSecurityScheme)
                        .AddAuthorizationCodeFlow(GoogleOAuthSecurityScheme, flow =>
                        {
                            flow.ClientId = googleOAuthOptions.ClientId;
                            flow.ClientSecret = googleOAuthClientSecret;

                            flow.Pkce = Pkce.Sha256;

                            flow.SelectedScopes =
                            [
                                "openid",
                                "email",
                                "profile"
                            ];

                            flow.RedirectUri =
                                "http://localhost:8000/scalar/oauth/callback";
                            
                            flow.TokenName = "id_token"; // Using id token, not the access token
                        }))
            .AllowAnonymous();

        return app;
    }
    
    private static ScalarOptions WithOperationTitleSource(
        this ScalarOptions options,
        OperationTitleSource source)
    {
        options.OperationTitleSource = source;
        return options;
    }

    public static bool IsOpenApiGenerationLaunch(this Assembly? entryAssembly)
        => entryAssembly?.GetName().Name == "GetDocument.Insider";
}
