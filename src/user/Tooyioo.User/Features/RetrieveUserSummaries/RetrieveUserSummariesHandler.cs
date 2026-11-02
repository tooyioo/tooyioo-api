using System.Linq.Expressions;
using Slicent;
using Slicent.Application.Queries;
using Tooyioo.User.Features.Support;
// ReSharper disable InvertIf
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUserSummaries;

public sealed class RetrieveUserSummariesHandler
    : IQueryHandler<RetrieveUserSummariesQuery, RetrieveUserSummariesQueryResult>
{
    private readonly QueryService<UserDocument, RetrieveUserSummariesQuery> _queryService;

    public RetrieveUserSummariesHandler(QueryService<UserDocument, RetrieveUserSummariesQuery> queryService)
    {
        _queryService = queryService;
    }

    public async Task<RetrieveUserSummariesQueryResult> Handle(
        RetrieveUserSummariesQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagedResult = await _queryService.Get(query, cancellationToken);
        return new RetrieveUserSummariesQueryResult(pagedResult);
    }
}

public sealed record RetrieveUserSummariesQuery
    : DocumentFilter<UserDocument>, IQuery<RetrieveUserSummariesQueryResult>
{
    public string? Id { get; init; }
    public string? Alias { get; init; }

    public override Expression<Func<UserDocument, bool>> Filter
    {
        get
        {
            var filter = base.Filter.AndAlso(x => x.Status == UserDocument.StatusCompleted);

            if (Id is not null)
            {
                filter = filter.AndAlso(x => x.Id.Contains(Id, StringComparison.CurrentCultureIgnoreCase));
            }

            if (Alias is not null)
            {
                filter = filter.AndAlso(x => x.Alias!.Contains(Alias, StringComparison.CurrentCultureIgnoreCase));
            }

            return filter;
        }
    }
}

public sealed record RetrieveUserSummariesQueryResult(PagedResult<UserDocument> PagedResult);
