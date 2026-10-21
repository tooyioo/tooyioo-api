using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Queries;
using Tooyioo.UserOnboarding.Features.RetrieveUserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.RetrieveUserOnboarding.Support;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global

namespace Tooyioo.UserOnboarding.Features.RetrieveUserOnboarding;

public static class RetrieveUserOnboardingMappers
{
    public sealed class QueryMapper
        : IQueryMapper<RetrieveUserOnboardingRoute, HttpContext, RetrieveUserOnboardingQuery>
    {
        public RetrieveUserOnboardingQuery Map(RetrieveUserOnboardingRoute urlParams, HttpContext context)
            => new(new UserOnboardingId(urlParams.UserOnboardingId.ToString()));
    }

    public sealed class QueryHttpResponseMapper
        : IQueryHttpResponseMapper<RetrieveUserOnboardingQueryResult, HttpContext, RetrieveUserOnboardingResponse>
    {
        public IResult Map(
            RetrieveUserOnboardingQueryResult queryResult, 
            HttpContext context,
            QueryHttpResponseGenerator<RetrieveUserOnboardingResponse> queryHttpResponseGenerator)
            => queryResult.Match<IResult>(
                ok => queryHttpResponseGenerator.Ok(new RetrieveUserOnboardingResponse
                {
                    UserOnboardingId = ok.Document.Id,
                    Alias = ok.Document.Alias,
                    Name = ok.Document.Name,
                    LastName = ok.Document.LastName,
                    Email = ok.Document.Email,
                    IsEmailVerified = ok.Document.IsEmailVerified,
                    CancellationReason = ok.Document.CancellationReason,
                    Status = ToStatus(ok.Document.Status),
                    CreatedAt = ok.Document.CreatedAt,
                    LastModifiedAt = ok.Document.LastModifiedAt,
                    CompletedAt = ok.Document.CompletedAt,
                    CanceledAt = ok.Document.CanceledAt
                }),
                notFound => queryHttpResponseGenerator.NotFound(
                    context,
                    "user_onboarding_not_found",
                    $"Could not find user onboarding with Id {notFound.Id}"));

        private static string ToStatus(string storedStatus) =>
            storedStatus switch
            {
                UserOnboardingProjector.InProgress => RetrieveUserOnboardingResponse.StatusInProgress,
                UserOnboardingProjector.Completed  => RetrieveUserOnboardingResponse.StatusCompleted,
                UserOnboardingProjector.Canceled   => RetrieveUserOnboardingResponse.StatusCanceled,
                _ => throw new ArgumentOutOfRangeException(nameof(storedStatus), storedStatus, null)
            };
    }
}
