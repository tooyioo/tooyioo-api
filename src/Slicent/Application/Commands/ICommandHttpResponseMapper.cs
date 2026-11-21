using Microsoft.AspNetCore.Http;

namespace Slicent.Application.Commands;

/// <summary>
/// Maps a command result to an HTTP result and declares the status codes the endpoint can return.
/// Implementations are resolved at endpoint mapping time to build OpenAPI metadata, so they must
/// be stateless and must not depend on the current request.
/// </summary>
public interface ICommandHttpResponseMapper<in TCommandResult, in TContext, THttpResponse>
    where THttpResponse : class
{
    HttpResponseMetadata Responses { get; }

    IResult Map(TCommandResult commandResult, TContext context, CommandHttpResponseGenerator<THttpResponse> commandHttpResponseGenerator);
}