using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Slicent.Application.Commands;
// ReSharper disable ClassNeverInstantiated.Local

namespace Slicent.Tests.Application.Commands;

public sealed class WhenCommandServicesAreRegistered
{
    [Test]
    public async Task Then_command_mapper_is_registered()
    {
        var services = new ServiceCollection();
        services.AddSlicent(typeof(WhenCommandServicesAreRegistered).Assembly);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var mapper = scope.ServiceProvider
            .GetRequiredService<ICommandMapper<TestRequest, HttpContext, TestCommand>>();

        var command = mapper.Map(new TestRequest { Name = "Jane" }, new DefaultHttpContext());

        await Assert.That(command.Name).IsEqualTo("Jane");
    }

    [Test]
    public async Task Then_command_response_mapper_is_registered()
    {
        var services = new ServiceCollection();
        services.AddSlicent(typeof(WhenCommandServicesAreRegistered).Assembly);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var mapper = scope.ServiceProvider
            .GetRequiredService<ICommandHttpResponseMapper<TestCommandResult, HttpContext, Ok<TestResponse>>>();

        await Assert.That(mapper).IsTypeOf<TestCommandResponseMapper>();
    }

    public sealed record TestCommand(string Name)
        : ICommand<TestCommandResult>;

    public sealed record TestCommandResult(string Id);

    public sealed record TestRequest
    {
        public required string Name { get; init; }
    }

    public sealed record TestResponse
    {
        public required string Id { get; init; }
    }

    public sealed class TestCommandMapper
        : ICommandMapper<TestRequest, HttpContext, TestCommand>
    {
        public TestCommand Map(TestRequest request, HttpContext context)
            => new(request.Name);
    }

    private sealed class TestCommandResponseMapper
        : ICommandHttpResponseMapper<TestCommandResult, HttpContext, Ok<TestResponse>>
    {
        public Ok<TestResponse> Map(
            TestCommandResult commandResult,
            HttpContext context)
            => TypedResults.Ok(new TestResponse { Id = commandResult.Id });
    }
}
