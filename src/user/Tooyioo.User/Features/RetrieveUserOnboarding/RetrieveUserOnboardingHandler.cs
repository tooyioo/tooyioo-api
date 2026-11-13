using Eventuous;
using Slicent.Application.Queries;
using Slicent.EventStore;
using Tooyioo.User.Features.Support;

// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUserOnboarding;

public sealed class RetrieveUserOnboardingHandler
    : IQueryHandler<RetrieveUserOnboardingQuery, RetrieveUserOnboardingQueryResult>
{
    private readonly IEventReader _eventReader;
    private readonly IUserPersonalDataDetailsRetriever _personalDataDetailsRetriever;

    public RetrieveUserOnboardingHandler(
        IEventReader eventReader,
        IUserPersonalDataDetailsRetriever personalDataDetailsRetriever)
    {
        _eventReader = eventReader;
        _personalDataDetailsRetriever = personalDataDetailsRetriever;
    }

    public async Task<RetrieveUserOnboardingQueryResult> Handle(
        RetrieveUserOnboardingQuery query,
        CancellationToken cancellationToken = default)
    {
        var userOnboarding =
            await _eventReader.LoadStateOrNew<UserOnboardingState, UserOnboardingId>(
                query.UserOnboardingId,
                cancellationToken);

        if (userOnboarding.Events.Length == 0
            || !userOnboarding.State.IsInitiated
            || string.IsNullOrWhiteSpace(userOnboarding.State.UserId))
        {
            return new RetrieveUserOnboardingQueryNotFoundResult(query.UserOnboardingId);
        }

        var personalData = await _personalDataDetailsRetriever.Retrieve(
            userOnboarding.State.UserId,
            cancellationToken);

        return new RetrieveUserOnboardingQueryOkResult(
            query.UserOnboardingId,
            userOnboarding.State,
            personalData);
    }
}

public sealed record RetrieveUserOnboardingQuery(UserOnboardingId UserOnboardingId)
    : IQuery<RetrieveUserOnboardingQueryResult>;

public union RetrieveUserOnboardingQueryResult(
    RetrieveUserOnboardingQueryOkResult,
    RetrieveUserOnboardingQueryNotFoundResult);

public sealed record RetrieveUserOnboardingQueryOkResult(
    UserOnboardingId UserOnboardingId,
    UserOnboardingState State,
    UserPersonalDataDetails? UserPersonalData);

public sealed record RetrieveUserOnboardingQueryNotFoundResult(UserOnboardingId UserOnboardingId);
