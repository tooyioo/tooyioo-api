using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Queries;
using Tooyioo.Common;
using Tooyioo.User.Features.RetrieveUser.Contract;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.RetrieveUser;

public static class RetrieveUserMappers
{
    public sealed class QueryMapper
        : IQueryMapper<RetrieveUserRoute, HttpContext, RetrieveUserQuery>
    {
        public RetrieveUserQuery Map(RetrieveUserRoute urlParams, HttpContext context)
            => new(new UserId(urlParams.UserId.ToString()));
    }

    public sealed class QueryHttpResponseMapper
        : IQueryHttpResponseMapper<RetrieveUserQueryResult, HttpContext, RetrieveUserResponse>
    {
        public IResult Map(
            RetrieveUserQueryResult queryResult,
            HttpContext context,
            QueryHttpResponseGenerator<RetrieveUserResponse> queryHttpResponseGenerator)
            => queryResult switch
            {
                RetrieveUserQueryOkResult ok => queryHttpResponseGenerator.Ok(
                    new RetrieveUserResponse
                    {
                        Id = ok.Document.Id,
                        Alias = ok.Document.Alias ?? string.Empty,
                        Name = ok.Document.Name,
                        LastName = ok.Document.LastName,
                        Email = ok.Document.Email,
                        IsEmailVerified = ok.Document.IsEmailVerified,
                        PhoneNumber = ok.Document.PhoneNumber,
                        Status = ok.Document.Status,
                        CreatedAt = ok.Document.CreatedAt,
                        LastModifiedAt = ok.Document.LastModifiedAt,
                        TermsOfServiceVersion = ok.Document.TermsOfServiceVersion ?? string.Empty,
                        PrivacyPolicyVersion = ok.Document.PrivacyPolicyVersion ?? string.Empty
                    }),
                RetrieveUserQueryNotFoundResult notFound => queryHttpResponseGenerator.NotFound(
                    context,
                    "profile_not_found",
                    $"Could not find Profile with Id {notFound.Id}")
            };
    }
}
