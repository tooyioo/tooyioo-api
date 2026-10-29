using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Queries;
using Tooyioo.Common;
using Tooyioo.User.Features.RetrieveUserSummaries.Contract;
// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.RetrieveUserSummaries;

public sealed class RetrieveUserSummariesEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapQuery<
                RetrieveUserSummariesRoute,
                RetrieveUserSummariesQuery,
                RetrieveUserSummariesQueryResult,
                RetrieveUserSummariesResponse>(
                "/users/summaries",
                cfg => cfg
                    .WithName("retrieve-user-summaries")
                    .WithSummary("List user summaries")
                    .WithDescription(
                        "Returns paginated user summaries containing IDs, aliases, and names. Supports filtering by ID and alias.")
                    .WithTags("users"))
            .RequireRouteAuthorization<RetrieveUserSummariesRoute, RetrieveUserSummariesAuthorizer>()
            .WithQueryConventions<RetrieveUserSummariesResponse, HttpProblemDetails>();
}
