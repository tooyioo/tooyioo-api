# Event sourcing

Tooyioo uses aggregate-less Event Sourcing.

The main modeling unit is a business process, not a long-lived entity.

## Process streams

An event stream represents the execution of a business process. It should answer:

> How is this process currently executing?

It should not answer:

> What happened to this entity across its entire lifetime?

Use process-oriented stream names. Prefer gerund-style process names where they fit the domain language.

Examples:

- `UserOnboarding`
- `ClaimingExternalIdentity`
- `ClaimingAlias`
- `CampaignConfiguring`
- `CampaignRunning`

Avoid entity-history streams such as:

- `User`
- `Organization`
- `Campaign`

## State

State reconstructed from a stream is short-lived decision state.

Its purpose is to decide whether the next command is valid and which events should be appended. It is not a read model and should not become a complete entity representation.

## Command handlers

Command handlers are the transactional boundary.

A command handler may:

- load one or more folded streams
- evaluate invariants
- decide which events should be appended
- append events to one or more streams with expected stream versions

Do not add aggregate classes. The aggregate responsibilities are fulfilled by:

- process streams
- folded state
- optimistic concurrency
- command handler decision logic

## Multi-stream writes

Use multi-stream writes when a single business decision must atomically update more than one consistency boundary.

For example, initiating user onboarding writes:

- the user onboarding process stream
- the external identity claiming process stream

Each stream must be written with the expected version returned by the load step.

## Idempotency

Idempotency should be modeled explicitly.

If a command observes that the intended business fact has already happened, the handler should return the existing result and append no new events.

Tests should assert this by comparing stream versions before and after the request.

## Domain events

Domain events describe facts that already happened.

Prefer names such as:

- `UserOnboardingInitiated`
- `UserOnboardingExternalIdentityAssociated`
- `UserOnboardingEmailVerified`
- `ExternalIdentityClaimed`

Avoid technical or CRUD-style names such as:

- `UserCreated`
- `DatabaseRecordInserted`
- `StatusUpdated`

Event schemas must remain backward compatible or have an explicit migration strategy.

Persisted event names and serialization are defined in [Slicent](slicent.md#event-serialization).
