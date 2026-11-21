using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
// ReSharper disable ConvertToExtensionBlock

namespace Slicent.Application.Queries;

public static class QueryEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a query endpoint. The response metadata comes from the mapper's typed result.
    /// </summary>
    public static RouteHandlerBuilder MapQuery<TQuery, TResult, THttpResult>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TQuery : IQuery<TResult>
        where THttpResult : IResult
    {
        var builder = app.MapGet(pattern, Handle);
        builder.WithErrorResponses();

        configure?.Invoke(builder);
        return builder;

        async Task<THttpResult> Handle(
            HttpContext ctx,
            IQueryMapper<HttpContext, TQuery> queryMapper,
            IQueryDispatcher dispatcher,
            IQueryHttpResponseMapper<TResult, HttpContext, THttpResult> responseMapper)
        {
            var query = queryMapper.Map(ctx);
            var result = await dispatcher.Send(query, ctx.RequestAborted);
            return responseMapper.Map(result, ctx);
        }
    }

    public static RouteHandlerBuilder MapQuery<TRoute, TQuery, TResult, THttpResult>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TRoute : notnull
        where TQuery : IQuery<TResult>
        where THttpResult : IResult
    {
        var builder = app.MapGet(pattern, Handle);
        builder.WithErrorResponses();

        configure?.Invoke(builder);
        return builder;

        async Task<THttpResult> Handle(
            [AsParameters] TRoute route,
            HttpContext ctx,
            IQueryMapper<TRoute, HttpContext, TQuery> queryMapper,
            IQueryDispatcher dispatcher,
            IQueryHttpResponseMapper<TResult, HttpContext, THttpResult> responseMapper)
        {
            var query = queryMapper.Map(route, ctx);
            var result = await dispatcher.Send(query, ctx.RequestAborted);
            return responseMapper.Map(result, ctx);
        }
    }
}
