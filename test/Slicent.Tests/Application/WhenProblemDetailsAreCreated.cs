using System.Net;
using Microsoft.AspNetCore.Http;
using Slicent.Application;

namespace Slicent.Tests.Application;

public sealed class WhenProblemDetailsAreCreated
{
    [Test]
    public async Task Then_conflict_response_contains_problem_details()
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/commands"
            }
        };

        var result = TypedResults.Conflict(HttpProblemDetails.Conflict(context, "already_exists", "Resource already exists."));

        await Assert.That(result).IsAssignableTo<IValueHttpResult<HttpProblemDetails>>();
        await Assert.That(result).IsAssignableTo<IStatusCodeHttpResult>();

        var valueResult = (IValueHttpResult<HttpProblemDetails>)result;
        var statusResult = (IStatusCodeHttpResult)result;

        await Assert.That(statusResult.StatusCode).IsEqualTo((int)HttpStatusCode.Conflict);
        await Assert.That(valueResult.Value!.Title).IsEqualTo("already_exists");
        await Assert.That(valueResult.Value.Detail).IsEqualTo("Resource already exists.");
        await Assert.That(valueResult.Value.Instance).IsEqualTo("/commands");
    }
}
