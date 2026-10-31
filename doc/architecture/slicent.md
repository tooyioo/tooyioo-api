# Slicent

Slicent is the local framework for Tooyioo vertical slice conventions.

It sits on top of Eventuous and ASP.NET Core conventions. It should not hide Eventuous or become a business framework.

## What belongs in Slicent

Add code to Slicent when it is:

- generic across modules
- about vertical slice registration or dispatch
- about command/query conventions
- about Eventuous integration
- independent from Tooyioo business concepts

Examples:

- command handler registration
- query handler registration
- endpoint mapping conventions
- event store helper methods
- framework-level testing helpers for Slicent itself

## What does not belong in Slicent

Do not add Tooyioo domain behavior to Slicent.

Avoid putting these in Slicent:

- user onboarding rules
- campaign rules
- merchant rules
- Tooyioo claim names
- module-specific event names
- module-specific read models

## Eventuous relationship

Slicent intentionally does not abstract Eventuous away.

Prefer exposing or composing Eventuous primitives when that keeps behavior clear:

- `IEventReader`
- `IEventWriter`
- folded state
- stream names
- expected stream versions

Only add wrapper APIs when they remove repeated ceremony without hiding important event-sourcing semantics.

## Event serialization

Slicent keeps event names within each module while using Eventuous's `ITypeMapper` and `IEventSerializer` contracts.

- Decorate each persisted event with `[DomainEventType("V1.EventName")]`. The name is a stored contract and must remain stable.
- Pass that contracts assembly to `AddSlicent`. It scans only the supplied assemblies, checks for duplicate names, and registers a fresh `TypeMapper` and reflection-based JSON serializer for that service provider. JSON uses `JsonSerializerDefaults.Web` to match the stored format.
- Inject `ITypeMapper` into Eventuous projectors and pass it to their base constructor so handler registration uses the same host-specific map as the serializer.

Do not register these events in the static Eventuous `TypeMap` or set `EventSerializer.Default`. The Eventuous `EVTC001` analyzer recognizes only its own `[EventType]` attribute; projects using Slicent's registration suppress that diagnostic locally. Slicent's registration validates annotated events and duplicate stored names, but it cannot detect an event used by a handler that lacks `[DomainEventType]`. Keep serialization coverage for each persisted event so an omitted attribute is caught before deployment.

## Testing Slicent

See [Testing](../guides/testing.md) for test locations and hosts. Slicent tests should verify framework behavior without depending on Tooyioo application modules.
