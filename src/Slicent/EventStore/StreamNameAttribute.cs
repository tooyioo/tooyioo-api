namespace Slicent.EventStore;

/// <summary>
/// Declares the storage name (the stream category) of a state. The stream name is the category,
/// a dash and the state identity: <c>{name}-{id}</c>.
/// </summary>
/// <remarks>
/// The name is part of the storage contract, so it must not follow a class rename.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class StreamNameAttribute(string name) 
    : Attribute
{
    public string Name { get; } = name;
}
