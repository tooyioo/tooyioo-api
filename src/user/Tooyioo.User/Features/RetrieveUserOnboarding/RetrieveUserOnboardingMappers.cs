global using RetrieveUserOnboardingHttpResult =
    Microsoft.AspNetCore.Http.HttpResults.Results<
        Microsoft.AspNetCore.Http.HttpResults.Ok<Tooyioo.User.Features.RetrieveUserOnboarding.Contracts.RetrieveUserOnboardingResponse>,
        Microsoft.AspNetCore.Http.HttpResults.NotFound<Slicent.Application.HttpProblemDetails>>;

using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Queries;
using Tooyioo.User.Features.RetrieveUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.RetrieveUserOnboarding;

public static class RetrieveUserOnboardingMappers
{
    private const string ErasedValue = "[erased]";

    public sealed class QueryMapper
        : IQueryMapper<RetrieveUserOnboardingRoute, HttpContext, RetrieveUserOnboardingQuery>
    {
        public RetrieveUserOnboardingQuery Map(RetrieveUserOnboardingRoute urlParams, HttpContext context)
            => new(new UserId(urlParams.UserId.ToString()));
    }

    public sealed class QueryHttpResponseMapper
        : IQueryHttpResponseMapper<RetrieveUserOnboardingQueryResult, HttpContext, RetrieveUserOnboardingHttpResult>
    {
        public RetrieveUserOnboardingHttpResult Map(
            RetrieveUserOnboardingQueryResult queryResult,
            HttpContext context)
            => queryResult switch
            {
                RetrieveUserOnboardingQueryOkResult ok => TypedResults.Ok(
                    new RetrieveUserOnboardingResponse
                    {
                        UserId = ok.UserId.Value,
                        Alias = ok.State.Alias,
                        Name = ok.UserPersonalData?.Name ?? ErasedValue,
                        LastName = ok.UserPersonalData?.LastName ?? ErasedValue,
                        Email = ok.UserPersonalData?.Email ?? ErasedValue,
                        IsEmailVerified = ok.UserPersonalData?.IsEmailVerified ?? false,
                        CancellationReasonCode = ok.State.CancellationReasonCode,
                        Status = ToStatus(ok.State)
                    }),
                RetrieveUserOnboardingQueryNotFoundResult notFound => TypedResults.NotFound(HttpProblemDetails.NotFound(
                    context,
                    "user_onboarding_not_found",
                    $"Could not find user onboarding with Id {notFound.UserId}"))
            };

        private static string ToStatus(UserOnboardingState state) =>
            state.IsCanceled
                ? RetrieveUserOnboardingResponse.StatusCanceled
                : state.IsComplete
                    ? RetrieveUserOnboardingResponse.StatusCompleted
                    : RetrieveUserOnboardingResponse.StatusInProgress;
    }
}
