using Eventuous;

// ReSharper disable ConvertToExtensionBlock
// ReSharper disable InvertIf

namespace Slicent.EventStore;

public static class EventStoreExtensions
{
    public static Task<FoldedEventStream<TState>> LoadStateOrNew<TState, TId>(
        this IEventReader eventReader,
        TId id,
        CancellationToken ct)
        where TState : State<TState>, IStreamState<TId>, new()
        where TId : Id =>
        eventReader.LoadState<TState>(
            StreamNames.For<TState>(TState.StreamIdentity(id)),
            failIfNotFound: false,
            cancellationToken: ct);
    
    public static StateStreamChanges ToStateStreamChanges<TState>(
        this FoldedEventStream<TState> stream,
        IReadOnlyCollection<object> events)
        where TState : State<TState>, new()
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(events);

        return new StateStreamChanges(stream.StreamName, stream.StreamVersion, events);
    }
    
    public static Task<AppendEventsResult[]> StoreStateChanges(
        this IEventWriter eventWriter,
        IReadOnlyCollection<StateStreamChanges> changes,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(eventWriter);
        ArgumentNullException.ThrowIfNull(changes);
        
        var streams = changes
            .Where(change => change.Events.Count > 0)
            .Select(change => (change.StreamName, change.ExpectedVersion, change.Events))
            .ToList();

        return streams.Count == 0
            ? Task.FromResult(Array.Empty<AppendEventsResult>())
            : eventWriter.Store(streams, cancellationToken: ct);
    }
}

public sealed record StateStreamChanges(
    StreamName StreamName,
    ExpectedStreamVersion ExpectedVersion,
    IReadOnlyCollection<object> Events);
