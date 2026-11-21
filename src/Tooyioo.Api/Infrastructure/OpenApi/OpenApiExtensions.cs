using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Tooyioo.Api.Infrastructure.OpenApi;

public static class OpenApiExtensions
{
    private const string DocumentName = "public";
    private const string GoogleOAuthSecurityScheme = "GoogleOAuth";

    // Shared, generic examples. Titles for 404/409/422 vary by endpoint, so their sample_ values
    // make clear that they are illustrative rather than endpoint-specific contracts.
    private static readonly IReadOnlyDictionary<string, string> ProblemDetailsExamples =
        new Dictionary<string, string>
        {
            ["400"] = """
                {
                  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                  "title": "validation_failed",
                  "status": 400,
                  "detail": "One or more validation errors occurred.",
                  "instance": "/sample-resources/00000000-0000-0000-0000-000000000010",
                  "traceId": "0HN7A1B2C3D4E:00000001",
                  "activityTraceId": "4bf92f3577b34da6a3ce929d0e0e4736",
                  "errors": {
                    "sample_field": ["sample_validation_error"]
                  }
                }
                """,
            ["401"] = """
                {
                  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
                  "title": "unauthorized",
                  "status": 401,
                  "detail": "Missing or invalid bearer token.",
                  "instance": "/sample-resources/00000000-0000-0000-0000-000000000010",
                  "traceId": "0HN7A1B2C3D4E:00000002",
                  "activityTraceId": "4bf92f3577b34da6a3ce929d0e0e4736"
                }
                """,
            ["403"] = """
                {
                  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4",
                  "title": "forbidden",
                  "status": 403,
                  "detail": "You are not allowed to access this resource.",
                  "instance": "/sample-resources/00000000-0000-0000-0000-000000000010",
                  "traceId": "0HN7A1B2C3D4E:00000003",
                  "activityTraceId": "4bf92f3577b34da6a3ce929d0e0e4736"
                }
                """,
            ["404"] = """
                {
                  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
                  "title": "sample_not_found",
                  "status": 404,
                  "detail": "The requested sample resource was not found.",
                  "instance": "/sample-resources/00000000-0000-0000-0000-000000000010",
                  "traceId": "0HN7A1B2C3D4E:00000004",
                  "activityTraceId": "4bf92f3577b34da6a3ce929d0e0e4736"
                }
                """,
            ["409"] = """
                {
                  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
                  "title": "sample_conflict",
                  "status": 409,
                  "detail": "The request conflicts with the current state. Refresh and retry.",
                  "instance": "/sample-resources/00000000-0000-0000-0000-000000000010",
                  "traceId": "0HN7A1B2C3D4E:00000005",
                  "activityTraceId": "4bf92f3577b34da6a3ce929d0e0e4736"
                }
                """,
            ["422"] = """
                {
                  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.21",
                  "title": "sample_unprocessable_entity",
                  "status": 422,
                  "detail": "The request is well-formed but cannot be processed in the current state.",
                  "instance": "/sample-resources/00000000-0000-0000-0000-000000000010",
                  "traceId": "0HN7A1B2C3D4E:00000006",
                  "activityTraceId": "4bf92f3577b34da6a3ce929d0e0e4736"
                }
                """
        };

    public static TBuilder AddOpenApi<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddOptions<ScalarGoogleOAuthOptions>()
            .BindConfiguration(ScalarGoogleOAuthOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.AddOpenApi(DocumentName, options =>
        {
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
                AddProblemDetailsExamples(operation);

                return Task.CompletedTask;
            });
        });

        return builder;
    }

    private static void AddProblemDetailsExamples(OpenApiOperation operation)
    {
        if (operation.Responses is null)
        {
            return;
        }

        foreach (var status in ProblemDetailsExamples.Keys)
        {
            if (!operation.Responses.TryGetValue(status, out var problemResponse)
                || problemResponse is not OpenApiResponse response
                || response.Content is null
                || !response.Content.TryGetValue("application/problem+json", out var content)
                || content is not OpenApiMediaType mediaType
                || mediaType.Example is not null
                || mediaType.Examples is { Count: > 0 })
            {
                continue;
            }

            mediaType.Example = JsonNode.Parse(ProblemDetailsExamples[status]);
        }
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
        if (!app.Environment.IsDevelopment() || Assembly.GetEntryAssembly().IsOpenApiGenerationLaunch())
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
