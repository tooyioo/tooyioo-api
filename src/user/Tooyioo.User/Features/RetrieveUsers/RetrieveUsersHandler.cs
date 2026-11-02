using System.Linq.Expressions;
using Slicent;
using Slicent.Application.Queries;
using Tooyioo.User.Features.Support;
// ReSharper disable InvertIf
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUsers;

public sealed class RetrieveUsersHandler
    : IQueryHandler<RetrieveUsersQuery, RetrieveUsersQueryResult>
{
    private readonly QueryService<UserDocument, RetrieveUsersQuery> _queryService;

    public RetrieveUsersHandler(QueryService<UserDocument, RetrieveUsersQuery> queryService)
    {
        _queryService = queryService;
    }

    public async Task<RetrieveUsersQueryResult> Handle(
        RetrieveUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagedResult = await _queryService.Get(query, cancellationToken);
        return new RetrieveUsersQueryResult(pagedResult);
    }
}

public sealed record RetrieveUsersQuery
    : DocumentFilter<UserDocument>, IQuery<RetrieveUsersQueryResult>
{
    public string? Id { get; init; }
    public string? Alias { get; init; }
    public string? Name { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public bool? IsEmailVerified { get; init; }
    public DateTime? CreatedAtFrom { get; init; }
    public DateTime? CreatedAtTo { get; init; }
    public DateTime? LastModifiedAtFrom { get; init; }
    public DateTime? LastModifiedAtTo { get; init; }

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

            if (Name is not null)
            {
                filter = filter.AndAlso(x => x.Name.Contains(Name, StringComparison.CurrentCultureIgnoreCase));
            }

            if (LastName is not null)
            {
                filter = filter.AndAlso(x => x.LastName.Contains(LastName, StringComparison.CurrentCultureIgnoreCase));
            }

            if (Email is not null)
            {
                filter = filter.AndAlso(x => x.Email.Contains(Email, StringComparison.CurrentCultureIgnoreCase));
            }

            if (IsEmailVerified is not null)
            {
                filter = filter.AndAlso(x => x.IsEmailVerified == IsEmailVerified);
            }

            if (CreatedAtFrom.HasValue)
            {
                var from = DateTime.SpecifyKind(CreatedAtFrom.Value, DateTimeKind.Utc);
                filter = filter.AndAlso(x => x.CreatedAt >= from);
            }

            if (CreatedAtTo.HasValue)
            {
                var to = DateTime.SpecifyKind(CreatedAtTo.Value, DateTimeKind.Utc);
                filter = filter.AndAlso(x => x.CreatedAt <= to);
            }

            if (LastModifiedAtFrom.HasValue)
            {
                var from = DateTime.SpecifyKind(LastModifiedAtFrom.Value, DateTimeKind.Utc);
                filter = filter.AndAlso(x => x.LastModifiedAt >= from);
            }

            if (LastModifiedAtTo.HasValue)
            {
                var to = DateTime.SpecifyKind(LastModifiedAtTo.Value, DateTimeKind.Utc);
                filter = filter.AndAlso(x => x.LastModifiedAt <= to);
            }

            return filter;
        }
    }
}

public sealed record RetrieveUsersQueryResult(PagedResult<UserDocument> PagedResult);
