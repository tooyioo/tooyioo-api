using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Queries;
using Tooyioo.Common;
using Tooyioo.User.Features.RetrieveUsers.Contract;
// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.RetrieveUsers;

public sealed class RetrieveUsersEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapQuery<
                RetrieveUsersRoute,
                RetrieveUsersQuery,
                RetrieveUsersQueryResult,
                RetrieveUsersResponse>(
                "/users",
                cfg => cfg
                    .WithName("retrieve-users")
                    .WithSummary("List users")
                    .WithDescription(
                        "Returns paginated users and supports filtering by profile, identity, email verification, and date ranges.")
                    .WithTags("users"))
            .RequireRouteAuthorization<RetrieveUsersRoute, RetrieveUsersAuthorizer>()
            .WithQueryConventions<RetrieveUsersResponse, HttpProblemDetails>();
}
