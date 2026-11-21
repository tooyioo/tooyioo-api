using Microsoft.AspNetCore.Http;

namespace Slicent.Application.Queries;

/// <summary>
/// Maps a query result to an HTTP result and declares the status codes the endpoint can return.
/// Implementations are resolved at endpoint mapping time to build OpenAPI metadata, so they must
/// be stateless and must not depend on the current request.
/// </summary>
public interface IQueryHttpResponseMapper<in TQueryResult, in TContext, THttpResponse>
    where THttpResponse : class
{
    HttpResponseMetadata Responses { get; }

    IResult Map(TQueryResult queryResult, TContext context, QueryHttpResponseGenerator<THttpResponse> queryHttpResponseGenerator);
}