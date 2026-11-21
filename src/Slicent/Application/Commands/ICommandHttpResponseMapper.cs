using Microsoft.AspNetCore.Http;

namespace Slicent.Application.Commands;

/// <summary>
/// Maps a command result to a typed HTTP result. The typed result (or a Results union) is the
/// source for the endpoint OpenAPI metadata; implementations should be stateless because the
/// endpoint is built from their static return type.
/// </summary>
public interface ICommandHttpResponseMapper<in TCommandResult, in TContext, out THttpResult>
    where THttpResult : IResult
{
    THttpResult Map(TCommandResult commandResult, TContext context);
}
