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
                    UserId = ok.State.UserId!,
                    UserOnboardingId = ok.UserOnboardingId.Value,
                    Alias = ok.State.Alias,
                    Name = ok.UserPersonalData?.Name ?? ErasedValue,
                    LastName = ok.UserPersonalData?.LastName ?? ErasedValue,
                    Email = ok.UserPersonalData?.Email ?? ErasedValue,
                    IsEmailVerified = ok.UserPersonalData?.IsEmailVerified ?? false,
                    CancellationReasonCode = ok.State.CancellationReasonCode,
                    Status = ToStatus(ok.State)
                }),
                notFound => queryHttpResponseGenerator.NotFound(
                    context,
                    "user_onboarding_not_found",
                    $"Could not find user onboarding with Id {notFound.UserOnboardingId}"));

        private static string ToStatus(UserOnboardingState state) =>
            state.IsCanceled
                ? RetrieveUserOnboardingResponse.StatusCanceled
                : state.IsComplete
                    ? RetrieveUserOnboardingResponse.StatusCompleted
                    : RetrieveUserOnboardingResponse.StatusInProgress;
    }
}
