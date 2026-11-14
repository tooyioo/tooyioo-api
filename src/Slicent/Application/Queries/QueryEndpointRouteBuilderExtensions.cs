using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
// ReSharper disable ConvertToExtensionBlock

namespace Slicent.Application.Queries;

public static class QueryEndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder MapQuery<TQuery, TResult, TResponse>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TQuery : IQuery<TResult>
        where TResponse : class
    {
        var builder = app.MapGet(pattern, async (
            HttpContext ctx,
            IQueryMapper<HttpContext, TQuery> queryMapper,
            IQueryDispatcher dispatcher,
            IQueryHttpResponseMapper<TResult, HttpContext, TResponse> responseMapper,
            QueryHttpResponseGenerator<TResponse> gen) =>
        {
            var query = queryMapper.Map(ctx);
            var result = await dispatcher.Send(query, ctx.RequestAborted);
            return responseMapper.Map(result, ctx, gen);
        });

        builder.WithQueryResponses<TResponse>();

        configure?.Invoke(builder);
        return builder;
    }

    public static RouteHandlerBuilder MapQuery<TRoute, TQuery, TResult, TResponse>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TRoute : notnull
        where TQuery : IQuery<TResult>
        where TResponse : class
    {
        var builder = app.MapGet(pattern, async (
            [AsParameters] TRoute route,
            HttpContext ctx,
            IQueryMapper<TRoute, HttpContext, TQuery> queryMapper,
            IQueryDispatcher dispatcher,
            IQueryHttpResponseMapper<TResult, HttpContext, TResponse> responseMapper,
            QueryHttpResponseGenerator<TResponse> gen) =>
        {
            var query = queryMapper.Map(route, ctx);
            var result = await dispatcher.Send(query, ctx.RequestAborted);
            return responseMapper.Map(result, ctx, gen);
        });

        builder.WithQueryResponses<TResponse>();

        configure?.Invoke(builder);
        return builder;
    }
}

