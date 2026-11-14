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
    /// <summary>
    /// Maps a command that creates a resource: the server mints the identity and the response is
    /// <c>201 Created</c> with a <c>Location</c> header.
    /// </summary>
    public static RouteHandlerBuilder MapCreateCommand<TRequest, TCommand, TResult, TResponse>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TRequest : class
        where TCommand : ICommand<TResult>
        where TResponse : class
    {
        var builder = app.MapPost(pattern, Handle);
        builder.WithCommandResponses<TResponse>(StatusCodes.Status201Created);

        configure?.Invoke(builder);
        return builder;

        async Task<IResult> Handle(
            TRequest request,
            HttpContext ctx,
            ICommandMapper<TRequest, HttpContext, TCommand> commandMapper,
            ICommandDispatcher dispatcher,
            ICommandHttpResponseMapper<TResult, HttpContext, TResponse> responseMapper,
            CommandHttpResponseGenerator<TResponse> gen)
        {
            var command = commandMapper.Map(request, ctx);
            var result = await dispatcher.Send(command, ctx.RequestAborted);
            return responseMapper.Map(result, ctx, gen);
        }
    }

    public static RouteHandlerBuilder MapCommand<TRequest, TCommand, TResult, TResponse>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null,
        CommandHttpMethod method = CommandHttpMethod.Post)
        where TRequest : class
        where TCommand : ICommand<TResult>
        where TResponse : class
    {
        var builder = Map(app, method, pattern, Handle);
        builder.WithCommandResponses<TResponse>(StatusCodes.Status200OK);

        configure?.Invoke(builder);
        return builder;

        async Task<IResult> Handle(
            TRequest request,
            HttpContext ctx,
            ICommandMapper<TRequest, HttpContext, TCommand> commandMapper,
            ICommandDispatcher dispatcher,
            ICommandHttpResponseMapper<TResult, HttpContext, TResponse> responseMapper,
            CommandHttpResponseGenerator<TResponse> gen)
        {
            var command = commandMapper.Map(request, ctx);
            var result = await dispatcher.Send(command, ctx.RequestAborted);
            return responseMapper.Map(result, ctx, gen);
        }
    }

    public static RouteHandlerBuilder MapCommand<TRoute, TRequest, TCommand, TResult, TResponse>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null,
        CommandHttpMethod method = CommandHttpMethod.Post)
        where TRoute : notnull
        where TRequest : class
        where TCommand : ICommand<TResult>
        where TResponse : class
    {
        var builder = Map(app, method, pattern, Handle);
        builder.WithCommandResponses<TResponse>(StatusCodes.Status200OK);

        configure?.Invoke(builder);
        return builder;

        async Task<IResult> Handle(
            [AsParameters] TRoute route,
            TRequest request,
            HttpContext ctx,
            ICommandMapper<TRoute, TRequest, HttpContext, TCommand> commandMapper,
            ICommandDispatcher dispatcher,
            ICommandHttpResponseMapper<TResult, HttpContext, TResponse> responseMapper,
            CommandHttpResponseGenerator<TResponse> gen)
        {
            var command = commandMapper.Map(route, request, ctx);
            var result = await dispatcher.Send(command, ctx.RequestAborted);
            return responseMapper.Map(result, ctx, gen);
        }
    }

    /// <summary>
    /// Maps a command that has no request body, only route parameters. Used by resource deletions.
    /// </summary>
    public static RouteHandlerBuilder MapDelete<TRoute, TCommand, TResult, TResponse>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TRoute : notnull
        where TCommand : ICommand<TResult>
        where TResponse : class
    {
        var builder = app.MapDelete(pattern, Handle);
        builder.WithCommandResponses<TResponse>(StatusCodes.Status200OK);

        configure?.Invoke(builder);
        return builder;

        async Task<IResult> Handle(
            [AsParameters] TRoute route,
            HttpContext ctx,
            ICommandMapper<TRoute, HttpContext, TCommand> commandMapper,
            ICommandDispatcher dispatcher,
            ICommandHttpResponseMapper<TResult, HttpContext, TResponse> responseMapper,
            CommandHttpResponseGenerator<TResponse> gen)
        {
            var command = commandMapper.Map(route, ctx);
            var result = await dispatcher.Send(command, ctx.RequestAborted);
            return responseMapper.Map(result, ctx, gen);
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
