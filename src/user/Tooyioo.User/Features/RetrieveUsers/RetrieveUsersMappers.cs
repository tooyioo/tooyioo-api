using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Queries;
using Tooyioo.User.Features.RetrieveUsers.Contract;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.RetrieveUsers;

public static class RetrieveUsersMappers
{
    public sealed class QueryMapper
        : IQueryMapper<RetrieveUsersRoute, HttpContext, RetrieveUsersQuery>
    {
        public RetrieveUsersQuery Map(RetrieveUsersRoute route, HttpContext context)
            => new()
            {
                Id = route.Id,
                Alias = route.Alias,
                Name = route.Name,
                LastName = route.LastName,
                Email = route.Email,
                IsEmailVerified = route.IsEmailVerified,
                PageNumber = route.PageNumber ?? 1,
                PageSize = route.PageSize ?? 20,
                CreatedAtFrom = route.CreatedAtFrom,
                CreatedAtTo = route.CreatedAtTo,
                LastModifiedAtFrom = route.LastModifiedAtFrom,
                LastModifiedAtTo = route.LastModifiedAtTo
            };
    }

    public sealed class QueryHttpResponseMapper
        : IQueryHttpResponseMapper<RetrieveUsersQueryResult, HttpContext, RetrieveUsersResponse>
    {
        public IResult Map(
            RetrieveUsersQueryResult queryResult,
            HttpContext context,
            QueryHttpResponseGenerator<RetrieveUsersResponse> queryHttpResponseGenerator)
        {
            var pagedResult = queryResult.PagedResult;

            return queryHttpResponseGenerator.Ok(new RetrieveUsersResponse
            {
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize,
                TotalPages = pagedResult.TotalPages,
                TotalCount = pagedResult.TotalCount,
                Items = pagedResult.Items.Select(x
                    => new RetrieveUsersResponseItem
                    {
                        Id = x.Id,
                        Alias = x.Alias,
                        Name = x.Name,
                        LastName = x.LastName,
                        Email = x.Email,
                        IsEmailVerified = x.IsEmailVerified,
                        PhoneNumber = x.PhoneNumber,
                        ExternalId = x.ExternalIdentityId,
                        ExternalProvider = x.ExternalIdentityProvider,
                        UserOnboardingId = x.UserOnboardingId,
                        CreatedAt = x.CreatedAt,
                        LastModifiedAt = x.LastModifiedAt,
                        TermsAndConditionsVersion = x.TermsAndConditionsVersion
                    })
            });
        }
    }
}
