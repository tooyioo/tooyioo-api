using Microsoft.AspNetCore.Http;

namespace Slicent.Application.Queries;

/// <summary>
/// Maps a query result to a typed HTTP result. The typed result (or a Results union) is the
/// source for the endpoint OpenAPI metadata; implementations should be stateless because the
/// endpoint is built from their static return type.
/// </summary>
public interface IQueryHttpResponseMapper<in TQueryResult, in TContext, out THttpResult>
    where THttpResult : IResult
{
    THttpResult Map(TQueryResult queryResult, TContext context);
}
