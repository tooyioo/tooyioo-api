namespace Tooyioo.Common;

/// <summary>
/// Base for identifiers that are plain values of the owning module.
/// </summary>
/// <remarks>
/// Use it when the identifier never keys an event stream. Identifiers that do key a stream derive from
/// the event-store identity type instead, so the framework can address the stream.
/// </remarks>
public abstract record Identifier
{
    protected Identifier(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public sealed override string ToString() => Value;

    public static implicit operator string(Identifier id) => id.Value;
}
