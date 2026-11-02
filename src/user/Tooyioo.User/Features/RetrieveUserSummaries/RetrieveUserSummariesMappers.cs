using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Queries;
using Tooyioo.User.Features.RetrieveUserSummaries.Contract;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.RetrieveUserSummaries;

public static class RetrieveUserSummariesMappers
{
    public sealed class QueryMapper
        : IQueryMapper<RetrieveUserSummariesRoute, HttpContext, RetrieveUserSummariesQuery>
    {
        public RetrieveUserSummariesQuery Map(RetrieveUserSummariesRoute route, HttpContext context)
            => new()
            {
                Id = route.Id,
                Alias = route.Alias,
                PageNumber = route.PageNumber ?? 1,
                PageSize = route.PageSize ?? 20
            };
    }

    public sealed class QueryHttpResponseMapper
        : IQueryHttpResponseMapper<RetrieveUserSummariesQueryResult, HttpContext, RetrieveUserSummariesResponse>
    {
        public IResult Map(
            RetrieveUserSummariesQueryResult queryResult,
            HttpContext context,
            QueryHttpResponseGenerator<RetrieveUserSummariesResponse> queryHttpResponseGenerator)
        {
            var pagedResult = queryResult.PagedResult;

            return queryHttpResponseGenerator.Ok(new RetrieveUserSummariesResponse
            {
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize,
                TotalPages = pagedResult.TotalPages,
                TotalCount = pagedResult.TotalCount,
                Items = pagedResult.Items.Select(x
                    => new RetrieveUserSummariesResponseItem
                    {
                        Id = x.Id,
                        Alias = x.Alias ?? string.Empty,
                        Name = x.Name,
                        LastName = x.LastName
                    })
            });
        }
    }
}
