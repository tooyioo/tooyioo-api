using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Queries;
using Tooyioo.Common;
using Tooyioo.User.Features.RetrieveUserPersonalData.Contracts;

namespace Tooyioo.User.Features.RetrieveUserPersonalData;

public sealed class RetrieveUserPersonalDataEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapQuery<
                RetrieveUserPersonalDataRoute,
                RetrieveUserPersonalDataQuery,
                RetrieveUserPersonalDataQueryResult,
                RetrieveUserPersonalDataResponse>(
                "/users/{id:guid}/personal-data",
                cfg => cfg
                    .WithName("retrieve-user-personal-data")
                    .WithSummary("Get personal data")
                    .WithDescription("Returns the authenticated user's personal data and erasure status.")
                    .WithTags("user-personal-data"))
            .RequireRouteAuthorization<RetrieveUserPersonalDataRoute, RetrieveUserPersonalDataAuthorizer>()
            .WithQueryConventions<RetrieveUserPersonalDataResponse, HttpProblemDetails>();
}
