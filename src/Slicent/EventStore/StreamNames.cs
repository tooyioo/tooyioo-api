using System.Collections.Concurrent;
using System.Reflection;
using Eventuous;

namespace Slicent.EventStore;

/// <summary>
/// Resolves the stream name of a state from its <see cref="StreamNameAttribute"/> declaration.
/// </summary>
public static class StreamNames
{
    private static readonly ConcurrentDictionary<Type, string> Categories = new();

    public static StreamName For<TState>(string id)
        where TState : State<TState>
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException(
                $"The identity of {typeof(TState).Name} rendered an empty value. Its id type must render the value with ToString().");
        }

        return new StreamName($"{CategoryOf(typeof(TState))}-{id}");
    }

    private static string CategoryOf(Type stateType)
        => Categories.GetOrAdd(stateType, static type =>
        {
            var attribute = type.GetCustomAttribute<StreamNameAttribute>();

            if (attribute is null)
            {
                throw new InvalidOperationException(
                    $"State type {type.Name} must declare [{nameof(StreamNameAttribute)}] to name its stream.");
            }

            if (string.IsNullOrWhiteSpace(attribute.Name) || attribute.Name.Contains('-'))
            {
                throw new InvalidOperationException(
                    $"The stream name declared by {type.Name} must be non-empty and cannot contain a dash.");
            }

            return attribute.Name;
        });
}
