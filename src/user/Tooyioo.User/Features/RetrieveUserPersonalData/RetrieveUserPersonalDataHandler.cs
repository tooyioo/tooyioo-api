using Slicent.Application.Queries;
using Tooyioo.User.Features.Support;
using Tooyioo.Common;

// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUserPersonalData;

public sealed class RetrieveUserPersonalDataHandler
    : IQueryHandler<RetrieveUserPersonalDataQuery, RetrieveUserPersonalDataQueryResult>
{
    private readonly QueryService<UserPersonalDataDocument, RetrieveUserPersonalDataQuery> _queryService;

    public RetrieveUserPersonalDataHandler(
        QueryService<UserPersonalDataDocument, RetrieveUserPersonalDataQuery> queryService)
    {
        _queryService = queryService;
    }

    public async Task<RetrieveUserPersonalDataQueryResult> Handle(
        RetrieveUserPersonalDataQuery query,
        CancellationToken cancellationToken = default)
    {
        var document = await _queryService.GetSingleOrDefault(query.UserId, cancellationToken);

        return document is null
            ? new RetrieveUserPersonalDataQueryNotFoundResult(query.UserId)
            : new RetrieveUserPersonalDataQueryOkResult(document);
    }
}

public sealed record RetrieveUserPersonalDataQuery(UserId UserId)
    : DocumentFilter<UserPersonalDataDocument>, IQuery<RetrieveUserPersonalDataQueryResult>;

public union RetrieveUserPersonalDataQueryResult(
    RetrieveUserPersonalDataQueryOkResult,
    RetrieveUserPersonalDataQueryNotFoundResult);

public sealed record RetrieveUserPersonalDataQueryOkResult(UserPersonalDataDocument Document);
public sealed record RetrieveUserPersonalDataQueryNotFoundResult(UserId UserId);
