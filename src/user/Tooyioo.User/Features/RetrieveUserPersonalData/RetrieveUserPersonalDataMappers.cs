global using RetrieveUserPersonalDataHttpResult =
    Microsoft.AspNetCore.Http.HttpResults.Results<
        Microsoft.AspNetCore.Http.HttpResults.Ok<Tooyioo.User.Features.RetrieveUserPersonalData.Contracts.RetrieveUserPersonalDataResponse>,
        Microsoft.AspNetCore.Http.HttpResults.NotFound<Slicent.Application.HttpProblemDetails>>;

using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Queries;
using Tooyioo.User.Features.RetrieveUserPersonalData.Contracts;

namespace Tooyioo.User.Features.RetrieveUserPersonalData;

public static class RetrieveUserPersonalDataMappers
{
    public sealed class QueryMapper
        : IQueryMapper<RetrieveUserPersonalDataRoute, HttpContext, RetrieveUserPersonalDataQuery>
    {
        public RetrieveUserPersonalDataQuery Map(RetrieveUserPersonalDataRoute urlParams, HttpContext context)
            => new(new UserId(urlParams.UserId.ToString()));
    }

    public sealed class QueryHttpResponseMapper
        : IQueryHttpResponseMapper<RetrieveUserPersonalDataQueryResult, HttpContext, RetrieveUserPersonalDataHttpResult>
    {
        public RetrieveUserPersonalDataHttpResult Map(
            RetrieveUserPersonalDataQueryResult queryResult,
            HttpContext context)
            => queryResult switch
            {
                RetrieveUserPersonalDataQueryOkResult ok => TypedResults.Ok(
                    new RetrieveUserPersonalDataResponse
                    {
                        UserId = ok.Document.Id,
                        Name = ok.Document.Name,
                        LastName = ok.Document.LastName,
                        Email = ok.Document.Email,
                        IsEmailVerified = ok.Document.IsEmailVerified,
                        Status = ok.Document.Status,
                        EraseAfterUtc = ok.Document.EraseAfterUtc,
                        CreatedAt = ok.Document.CreatedAt,
                        LastModifiedAt = ok.Document.LastModifiedAt
                    }),
                RetrieveUserPersonalDataQueryNotFoundResult notFound => TypedResults.NotFound(HttpProblemDetails.NotFound(
                    context,
                    "user_personal_data_not_found",
                    $"Could not find personal data for user {notFound.UserId}"))
            };
    }
}
