using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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
    /// Maps a command that creates a resource. The response metadata, including the success status
    /// codes, comes from the command's response mapper.
    /// </summary>
    public static RouteHandlerBuilder MapCreateCommand<TRequest, TCommand, TResult, TResponse>(
        this IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TRequest : class
        where TCommand : ICommand<TResult>
        where TResponse : class
        => MapCreateCommandCore<TRequest, TCommand, TResult, TResponse>(
            app,
            pattern,
            configure);

    private static RouteHandlerBuilder MapCreateCommandCore<TRequest, TCommand, TResult, TResponse>(
        IEndpointRouteBuilder app,
        string pattern,
        Action<RouteHandlerBuilder>? configure)
        where TRequest : class
        where TCommand : ICommand<TResult>
        where TResponse : class
    {
        var builder = app.MapPost(pattern, Handle);
        builder.WithCommandResponses<TResponse>(ResolveResponses<TResult, TResponse>(app));

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
        builder.WithCommandResponses<TResponse>(ResolveResponses<TResult, TResponse>(app));

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
        builder.WithCommandResponses<TResponse>(ResolveResponses<TResult, TResponse>(app));

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
        builder.WithCommandResponses<TResponse>(ResolveResponses<TResult, TResponse>(app));

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

    private static HttpResponseMetadata ResolveResponses<TResult, TResponse>(IEndpointRouteBuilder app)
        where TResponse : class
    {
        using var scope = app.ServiceProvider.CreateScope();

        return scope.ServiceProvider
            .GetRequiredService<ICommandHttpResponseMapper<TResult, HttpContext, TResponse>>()
            .Responses;
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
