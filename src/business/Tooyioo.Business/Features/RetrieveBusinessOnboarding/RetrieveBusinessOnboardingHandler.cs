using Slicent.Application.Queries;
using Tooyioo.Business.Features.Support;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.Business.Features.RetrieveBusinessOnboarding;

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
            x => x.Id == query.BusinessId.Value && x.InitiatedBy == query.UserId.Value,
            cancellationToken);

        if (document is null)
        {
            return new RetrieveBusinessOnboardingQueryNotFoundResult(query.BusinessId);
        }

        return new RetrieveBusinessOnboardingQueryOkResult(document);
    }
}

public sealed record RetrieveBusinessOnboardingQuery(BusinessId BusinessId, BusinessUserId UserId)
    : DocumentFilter<BusinessOnboardingDocument>, IQuery<RetrieveBusinessOnboardingQueryResult>;

public union RetrieveBusinessOnboardingQueryResult(
    RetrieveBusinessOnboardingQueryOkResult,
    RetrieveBusinessOnboardingQueryNotFoundResult);

public sealed record RetrieveBusinessOnboardingQueryOkResult(BusinessOnboardingDocument Document);
public sealed record RetrieveBusinessOnboardingQueryNotFoundResult(BusinessId Id);
