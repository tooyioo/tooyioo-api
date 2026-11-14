namespace Slicent.EventStore;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DomainEventTypeAttribute(string name) 
    : Attribute
{
    public string Name { get; } = name;
}
