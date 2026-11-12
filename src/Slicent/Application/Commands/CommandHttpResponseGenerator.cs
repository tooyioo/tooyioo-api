using Microsoft.AspNetCore.Http;

namespace Slicent.Application.Commands;

public sealed class CommandHttpResponseGenerator<THttpResponse>
    where THttpResponse : class
{
    public IResult Ok(THttpResponse response) => TypedResults.Ok(response);

    public IResult BadRequest(HttpContext ctx, string title, string? detail = null)
        => TypedResults.BadRequest(HttpProblemDetails.BadRequest(ctx, title, detail));

    public IResult Conflict(HttpContext ctx, string title, string? detail = null)
        => TypedResults.Conflict(HttpProblemDetails.Conflict(ctx, title, detail));

    public IResult UnprocessableEntity(HttpContext ctx, string title, string? detail = null)
        => TypedResults.Problem(HttpProblemDetails.UnprocessableEntity(ctx, title, detail));

    public IResult NotFound(HttpContext ctx, string title, string? detail = null)
        => TypedResults.NotFound(HttpProblemDetails.NotFound(ctx, title, detail));

    public IResult Forbidden(HttpContext ctx, string title = "forbidden", string? detail = null)
        => TypedResults.Problem(HttpProblemDetails.FromForbidden(ctx, title, detail));
}
