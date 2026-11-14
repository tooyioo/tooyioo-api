using Eventuous;
using Slicent.EventStore;

namespace Tooyioo.Tests.Support.Events;

public sealed class EventAssertions(IEventReader eventReader)
{
    public EventStreamAssertions<TState, TId> Stream<TState, TId>(TId id)
        where TState : State<TState>, IStreamState<TId>, new()
        where TId : Id
        => new(eventReader, id);
}

public sealed class EventStreamAssertions<TState, TId>(IEventReader eventReader, TId id)
    where TState : State<TState>, IStreamState<TId>, new()
    where TId : Id
{
    public Task<EventStreamSnapshot> Capture()
        => CaptureSnapshot();

    public async Task<EventStreamSnapshot> CaptureSnapshot()
    {
        var folded = await LoadStream();
        return new EventStreamSnapshot(folded.StreamVersion, folded.Events);
    }

    public Task ShouldBeUnchangedFrom(EventStreamSnapshot snapshot)
        => ShouldHaveNoChangesSince(snapshot);

    public async Task ShouldHaveNoChangesSince(EventStreamSnapshot snapshot)
    {
        var folded = await LoadStream();
        await Assert.That(folded.StreamVersion).IsEqualTo(snapshot.StreamVersion);
        await ShouldContainExactly(folded.Events, snapshot.Events);
    }

    public async Task ShouldAppendExactly(EventStreamSnapshot snapshot, params object[] expected)
    {
        var folded = await LoadStream();
        var appended = folded.Events[snapshot.EventCount..];

        await Assert.That(folded.Events.Length).IsEqualTo(snapshot.EventCount + expected.Length);
        await ShouldContainExactly(folded.Events[..snapshot.EventCount], snapshot.Events);
        await ShouldContainExactly(appended, expected);
    }

    public async Task ShouldContain<TEvent>()
    {
        var events = await LoadEvents();
        await Assert.That(events.Any(evt => evt is TEvent)).IsTrue();
    }

    public async Task ShouldNotContain<TEvent>()
    {
        var events = await LoadEvents();
        await Assert.That(events.Any(evt => evt is TEvent)).IsFalse();
    }

    public async Task ShouldContainExactly(params object[] expected)
    {
        var events = await LoadEvents();
        await ShouldContainExactly(events, expected);
    }

    private static async Task ShouldContainExactly(object[] events, object[] expected)
    {
        await Assert.That(events.Length).IsEqualTo(expected.Length);

        for (var index = 0; index < expected.Length; index++)
        {
            await Assert.That(events[index]).IsEqualTo(expected[index]);
        }
    }

    public async Task<TState> ShouldHaveState()
    {
        var folded = await eventReader.LoadStateOrNew<TState, TId>(id, CancellationToken.None);
        await Assert.That(folded.Events.Length).IsGreaterThan(0);
        return folded.State;
    }

    private async Task<object[]> LoadEvents()
    {
        var folded = await LoadStream();
        return folded.Events;
    }

    private Task<FoldedEventStream<TState>> LoadStream()
        => eventReader.LoadStateOrNew<TState, TId>(id, CancellationToken.None);
}

public sealed record EventStreamSnapshot(ExpectedStreamVersion StreamVersion, object[] Events)
{
    public int EventCount => Events.Length;
}
