using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Queries;
using Tooyioo.BusinessOnboarding.Features.RetrieveBusinessOnboarding.Contracts;
using Tooyioo.BusinessOnboarding.Features.Support;
using Tooyioo.Common;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global

namespace Tooyioo.BusinessOnboarding.Features.RetrieveBusinessOnboarding;

public static class RetrieveBusinessOnboardingMappers
{
    public sealed class QueryMapper
        : IQueryMapper<RetrieveBusinessOnboardingRoute, HttpContext, RetrieveBusinessOnboardingQuery>
    {
        public RetrieveBusinessOnboardingQuery Map(
            RetrieveBusinessOnboardingRoute route,
            HttpContext context)
        {
            var userIdOption = context.User.GetClaim(Claims.UserId);
            if (!userIdOption.IsSome(out var userId))
            {
                throw new InvalidOperationException($"JWT is missing the '{Claims.UserId}' claim");
            }

            return new RetrieveBusinessOnboardingQuery(
                new BusinessOnboardingId(route.BusinessOnboardingId.ToString()),
                new UserId(userId));
        }
    }

    public sealed class QueryHttpResponseMapper
        : IQueryHttpResponseMapper<RetrieveBusinessOnboardingQueryResult, HttpContext,
            RetrieveBusinessOnboardingResponse>
    {
        public IResult Map(
            RetrieveBusinessOnboardingQueryResult queryResult,
            HttpContext context,
            QueryHttpResponseGenerator<RetrieveBusinessOnboardingResponse> queryHttpResponseGenerator)
            => queryResult.Match<IResult>(
                ok => queryHttpResponseGenerator.Ok(new RetrieveBusinessOnboardingResponse
                {
                    BusinessOnboardingId = ok.Document.Id,
                    LegalName = ok.Document.LegalName,
                    InitiatedBy = ok.Document.InitiatedBy,
                    Status = ToStatus(ok.Document.Status),
                    CreatedAt = ok.Document.CreatedAt,
                    LastModifiedAt = ok.Document.LastModifiedAt,
                    CompletedAt = ok.Document.CompletedAt,
                    CanceledAt = ok.Document.CanceledAt,
                    CancellationReason = ok.Document.CancellationReason
                }),
                notFound => queryHttpResponseGenerator.NotFound(
                    context,
                    "business_onboarding_not_found",
                    $"Could not find business onboarding with Id {notFound.Id}"));

        private static string ToStatus(string storedStatus) =>
            storedStatus switch
            {
                BusinessOnboardingDocument.StatusInProgress => RetrieveBusinessOnboardingResponse.StatusInProgress,
                BusinessOnboardingDocument.StatusCompleted => RetrieveBusinessOnboardingResponse.StatusCompleted,
                BusinessOnboardingDocument.StatusCanceled => RetrieveBusinessOnboardingResponse.StatusCanceled,
                _ => throw new ArgumentOutOfRangeException(nameof(storedStatus), storedStatus, null)
            };
    }
}
