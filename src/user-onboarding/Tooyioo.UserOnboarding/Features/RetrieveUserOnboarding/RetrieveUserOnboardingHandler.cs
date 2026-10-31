using Funzo;
using Slicent.Application.Queries;
using Tooyioo.UserOnboarding.Features.Support;

// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.UserOnboarding.Features.RetrieveUserOnboarding;

public sealed class RetrieveUserOnboardingHandler
    : IQueryHandler<RetrieveUserOnboardingQuery, RetrieveUserOnboardingQueryResult>
{
    private readonly QueryService<UserOnboardingDocument, RetrieveUserOnboardingQuery> _queryService;

    public RetrieveUserOnboardingHandler(QueryService<UserOnboardingDocument, RetrieveUserOnboardingQuery> queryService)
    {
        _queryService = queryService;
    }

    public async Task<RetrieveUserOnboardingQueryResult> Handle(
        RetrieveUserOnboardingQuery query, 
        CancellationToken cancellationToken = default)
    {
        var queryResult = await _queryService.GetSingleOrDefault(query.UserOnboardingId, cancellationToken);
        if (queryResult is null)
        {
            return new RetrieveUserOnboardingQueryNotFoundResult(query.UserOnboardingId);
        }
        return new RetrieveUserOnboardingQueryOkResult(queryResult);
    }
}

public sealed record RetrieveUserOnboardingQuery(UserOnboardingId UserOnboardingId)
    : DocumentFilter<UserOnboardingDocument>, IQuery<RetrieveUserOnboardingQueryResult>;

[Result<RetrieveUserOnboardingQueryOkResult, RetrieveUserOnboardingQueryNotFoundResult>]
public partial class RetrieveUserOnboardingQueryResult;

public sealed record RetrieveUserOnboardingQueryOkResult(UserOnboardingDocument Document);
public sealed record RetrieveUserOnboardingQueryNotFoundResult(UserOnboardingId Id);