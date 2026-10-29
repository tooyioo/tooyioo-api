using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Queries;
using Tooyioo.Common;
using Tooyioo.User.Features.RetrieveUser.Contract;

// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.RetrieveUser;

public sealed class RetrieveUserEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapQuery<
                RetrieveUserRoute,
                RetrieveUserQuery,
                RetrieveUserQueryResult,
                RetrieveUserResponse>(
                "/users/{id:guid}",
                cfg => cfg
                    .WithName("retrieve-user")
                    .WithSummary("Get a user")
                    .WithTags("users"))
            .RequireRouteAuthorization<RetrieveUserRoute, RetrieveUserAuthorizer>()
            .WithQueryConventions<RetrieveUserResponse, HttpProblemDetails>();
}
