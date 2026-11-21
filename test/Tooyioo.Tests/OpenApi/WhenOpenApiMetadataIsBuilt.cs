using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooyioo.Tests.Support;

namespace Tooyioo.Tests.OpenApi;

[Category(TestCategories.Infrastructure)]
public sealed class WhenOpenApiMetadataIsBuilt
{
    [Test]
    public async Task Then_typed_results_declare_the_endpoint_statuses()
    {
        await using var host = await VerticalSliceTestHost.Start();

        var endpoints = host.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        var initiate = endpoints.Single(endpoint =>
            endpoint.RoutePattern.RawText == "/users" &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains("POST"));

        var initiateStatuses = StatusesOf(initiate);
        await Assert.That(initiateStatuses.SetEquals([200, 201, 400, 401, 403, 409])).IsTrue();

        var retrieve = endpoints.Single(endpoint =>
            endpoint.RoutePattern.RawText == "/users/{id:guid}" &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains("GET"));

        var retrieveStatuses = StatusesOf(retrieve);
        await Assert.That(retrieveStatuses.SetEquals([200, 400, 401, 403, 404])).IsTrue();
    }

    private static HashSet<int> StatusesOf(RouteEndpoint endpoint)
        => endpoint.Metadata
            .OfType<ProducesResponseTypeMetadata>()
            .Select(metadata => metadata.StatusCode)
            .ToHashSet();
}
