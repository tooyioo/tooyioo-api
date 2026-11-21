using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
// ReSharper disable ConvertToExtensionBlock

namespace Slicent.Application.Commands;

public enum CommandHttpMethod
{
    Post,
    Put
}

public static class CommandEndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder MapCommand<TRequest, TCommand, TResult, THttpResult>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null,
        CommandHttpMethod method = CommandHttpMethod.Post)
        where TRequest : class
        where TCommand : ICommand<TResult>
        where THttpResult : IResult
    {
        var builder = Map(app, method, pattern, Handle);
        builder.WithErrorResponses();

        configure?.Invoke(builder);
        return builder;

        async Task<THttpResult> Handle(
            TRequest request,
            HttpContext ctx,
            ICommandMapper<TRequest, HttpContext, TCommand> commandMapper,
            ICommandDispatcher dispatcher,
            ICommandHttpResponseMapper<TResult, HttpContext, THttpResult> responseMapper)
        {
            var command = commandMapper.Map(request, ctx);
            var result = await dispatcher.Send(command, ctx.RequestAborted);
            return responseMapper.Map(result, ctx);
        }
    }

    public static RouteHandlerBuilder MapCommand<TRoute, TRequest, TCommand, TResult, THttpResult>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null,
        CommandHttpMethod method = CommandHttpMethod.Post)
        where TRoute : notnull
        where TRequest : class
        where TCommand : ICommand<TResult>
        where THttpResult : IResult
    {
        var builder = Map(app, method, pattern, Handle);
        builder.WithErrorResponses();

        configure?.Invoke(builder);
        return builder;

        async Task<THttpResult> Handle(
            [AsParameters] TRoute route,
            TRequest request,
            HttpContext ctx,
            ICommandMapper<TRoute, TRequest, HttpContext, TCommand> commandMapper,
            ICommandDispatcher dispatcher,
            ICommandHttpResponseMapper<TResult, HttpContext, THttpResult> responseMapper)
        {
            var command = commandMapper.Map(route, request, ctx);
            var result = await dispatcher.Send(command, ctx.RequestAborted);
            return responseMapper.Map(result, ctx);
        }
    }

    /// <summary>
    /// Maps a command that has no request body, only route parameters. Used by resource deletions.
    /// </summary>
    public static RouteHandlerBuilder MapDelete<TRoute, TCommand, TResult, THttpResult>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TRoute : notnull
        where TCommand : ICommand<TResult>
        where THttpResult : IResult
    {
        var builder = app.MapDelete(pattern, Handle);
        builder.WithErrorResponses();

        configure?.Invoke(builder);
        return builder;

        async Task<THttpResult> Handle(
            [AsParameters] TRoute route,
            HttpContext ctx,
            ICommandMapper<TRoute, HttpContext, TCommand> commandMapper,
            ICommandDispatcher dispatcher,
            ICommandHttpResponseMapper<TResult, HttpContext, THttpResult> responseMapper)
        {
            var command = commandMapper.Map(route, ctx);
            var result = await dispatcher.Send(command, ctx.RequestAborted);
            return responseMapper.Map(result, ctx);
        }
    }

    private static RouteHandlerBuilder Map(
        IEndpointRouteBuilder app,
        CommandHttpMethod method,
        string pattern,
        Delegate handler)
        => method switch
        {
            CommandHttpMethod.Put => app.MapPut(pattern, handler),
            _ => app.MapPost(pattern, handler)
        };
}
