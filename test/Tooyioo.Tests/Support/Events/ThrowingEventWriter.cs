using Eventuous;

namespace Tooyioo.Tests.Support.Events;

internal sealed class ThrowingEventWriter
    : IEventWriter
{
    public Task<AppendEventsResult> AppendEvents(
        StreamName stream,
        ExpectedStreamVersion expectedVersion,
        IReadOnlyCollection<NewStreamEvent> events,
        CancellationToken cancellationToken)
        => throw CreateConcurrencyException();

    public Task<AppendEventsResult[]> AppendEvents(
        IReadOnlyCollection<NewStreamAppend> streams,
        CancellationToken cancellationToken)
        => throw CreateConcurrencyException();

    private static OptimisticConcurrencyException CreateConcurrencyException()
        => new(
            new StreamName("user_onboarding/test"),
            new InvalidOperationException("Concurrent append simulated by the test"));
}
