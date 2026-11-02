using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Queries;
using Tooyioo.User.Features.RetrieveUserPersonalData.Contracts;
using Tooyioo.Common;

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
        : IQueryHttpResponseMapper<RetrieveUserPersonalDataQueryResult, HttpContext, RetrieveUserPersonalDataResponse>
    {
        public IResult Map(
            RetrieveUserPersonalDataQueryResult queryResult,
            HttpContext context,
            QueryHttpResponseGenerator<RetrieveUserPersonalDataResponse> queryHttpResponseGenerator)
            => queryResult.Match<IResult>(
                ok => queryHttpResponseGenerator.Ok(new RetrieveUserPersonalDataResponse
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
                notFound => queryHttpResponseGenerator.NotFound(
                    context,
                    "user_personal_data_not_found",
                    $"Could not find personal data for user {notFound.UserId}"));
    }
}
