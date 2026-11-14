using Eventuous;
using Slicent.EventStore;

namespace Slicent.Tests.EventStore;

public sealed class WhenStreamNamesAreResolved
{
    [Test]
    public async Task Then_the_name_comes_from_the_state_declaration()
    {
        var stream = StreamNames.For<FirstState>("user-1");

        await Assert.That(stream.ToString()).IsEqualTo("first_state-user-1");
    }

    [Test]
    public async Task Then_the_identity_is_read_back_from_the_stream_name()
    {
        var stream = StreamNames.For<FirstState>("user-1");

        await Assert.That(stream.GetId()).IsEqualTo("user-1");
    }

    [Test]
    public async Task Then_a_key_with_dashes_is_read_back_whole()
    {
        var stream = StreamNames.For<FirstState>("jane-bloggs-1984");

        await Assert.That(stream.GetId()).IsEqualTo("jane-bloggs-1984");
    }

    [Test]
    public async Task Then_the_state_declares_the_stream_identity()
    {
        var identity = IdentityOf<FirstState, TestId>(new TestId("user-1"));

        await Assert.That(identity).IsEqualTo("user-1");
    }

    [Test]
    public async Task Then_two_states_can_share_one_identity_type()
    {
        var first = StreamNames.For<FirstState>("user-1");
        var second = StreamNames.For<SecondState>("user-1");

        await Assert.That(first.ToString()).IsEqualTo("first_state-user-1");
        await Assert.That(second.ToString()).IsEqualTo("second_state-user-1");
    }

    [Test]
    public async Task Then_an_identity_that_renders_nothing_is_rejected()
        => await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromResult(StreamNames.For<FirstState>(string.Empty)));

    [Test]
    public async Task Then_a_state_without_a_declaration_fails_loudly()
        => await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromResult(StreamNames.For<UndeclaredState>("user-1")));

    [Test]
    public async Task Then_a_declaration_with_a_dash_is_rejected()
        => await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromResult(StreamNames.For<DashedState>("user-1")));

    private static string IdentityOf<TState, TId>(TId id)
        where TState : IStreamState<TId>
        where TId : Id
        => TState.StreamIdentity(id);

    [StreamName("first_state")]
    private sealed record FirstState : State<FirstState>, IStreamState<TestId>;

    [StreamName("second_state")]
    private sealed record SecondState : State<SecondState>, IStreamState<TestId>;

    private sealed record UndeclaredState : State<UndeclaredState>, IStreamState<TestId>;

    [StreamName("dashed-state")]
    private sealed record DashedState : State<DashedState>, IStreamState<TestId>;
}

public sealed record TestId(string Value) : Id(Value);
