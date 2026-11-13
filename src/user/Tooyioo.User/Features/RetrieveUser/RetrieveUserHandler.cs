using Slicent.Application.Queries;
using Tooyioo.Common;
using Tooyioo.User.Features.Support;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUser;

public sealed class RetrieveUserHandler
    : IQueryHandler<RetrieveUserQuery, RetrieveUserQueryResult>
{
    private readonly QueryService<UserDocument, RetrieveUserQuery> _queryService;

    public RetrieveUserHandler(QueryService<UserDocument, RetrieveUserQuery> queryService)
    {
        _queryService = queryService;
    }

    public async Task<RetrieveUserQueryResult> Handle(
        RetrieveUserQuery query, 
        CancellationToken cancellationToken = default)
    {
        var queryResult = await _queryService.GetSingleOrDefault(query.UserId, cancellationToken);
        if (queryResult is null)
        {
            return new RetrieveUserQueryNotFoundResult(query.UserId);
        }
        return new RetrieveUserQueryOkResult(queryResult);
    }
}

public sealed record RetrieveUserQuery(UserId UserId)
    : DocumentFilter<UserDocument>, IQuery<RetrieveUserQueryResult>;

public union RetrieveUserQueryResult(
    RetrieveUserQueryOkResult,
    RetrieveUserQueryNotFoundResult);

public sealed record RetrieveUserQueryOkResult(UserDocument Document);
public sealed record RetrieveUserQueryNotFoundResult(UserId Id);