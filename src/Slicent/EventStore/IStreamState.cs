using Eventuous;

namespace Slicent.EventStore;

/// <summary>
/// Binds a state to the identity type of its stream and turns that identity into the stream key.
/// </summary>
/// <remarks>
/// The identity is the event-store identity type, which already renders its value. States only declare the
/// binding; the default implementation is enough unless a state needs a different key.
/// The stream category comes from <see cref="StreamNameAttribute"/>, so several states can share one identity type.
/// </remarks>
public interface IStreamState<TId>
    where TId : Id
{
    /// <summary>
    /// The key that identifies the stream for the given identity. Defaults to the raw identity value.
    /// </summary>
    static virtual string StreamIdentity(TId id) => id.Value;
}
