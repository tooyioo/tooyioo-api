using Funzo;
using Slicent.Application.Queries;
using Tooyioo.BusinessOnboarding.Features.Support;
using Tooyioo.Common;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.BusinessOnboarding.Features.RetrieveBusinessOnboarding;

public sealed class RetrieveBusinessOnboardingHandler
    : IQueryHandler<RetrieveBusinessOnboardingQuery, RetrieveBusinessOnboardingQueryResult>
{
    private readonly QueryService<BusinessOnboardingDocument, RetrieveBusinessOnboardingQuery> _queryService;

    public RetrieveBusinessOnboardingHandler(
        QueryService<BusinessOnboardingDocument, RetrieveBusinessOnboardingQuery> queryService)
    {
        _queryService = queryService;
    }

    public async Task<RetrieveBusinessOnboardingQueryResult> Handle(
        RetrieveBusinessOnboardingQuery query,
        CancellationToken cancellationToken = default)
    {
        var document = await _queryService.GetSingleOrDefault(
            x => x.Id == query.BusinessOnboardingId.Value && x.InitiatedBy == query.UserId.Id,
            cancellationToken);

        if (document is null)
        {
            return new RetrieveBusinessOnboardingQueryNotFoundResult(query.BusinessOnboardingId);
        }

        return new RetrieveBusinessOnboardingQueryOkResult(document);
    }
}

public sealed record RetrieveBusinessOnboardingQuery(BusinessOnboardingId BusinessOnboardingId, UserId UserId)
    : DocumentFilter<BusinessOnboardingDocument>, IQuery<RetrieveBusinessOnboardingQueryResult>;

[Result<RetrieveBusinessOnboardingQueryOkResult, RetrieveBusinessOnboardingQueryNotFoundResult>]
public partial class RetrieveBusinessOnboardingQueryResult;

public sealed record RetrieveBusinessOnboardingQueryOkResult(BusinessOnboardingDocument Document);
public sealed record RetrieveBusinessOnboardingQueryNotFoundResult(BusinessOnboardingId Id);
