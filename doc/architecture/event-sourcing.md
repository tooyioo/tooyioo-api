# Event sourcing

Tooyioo uses aggregate-less Event Sourcing.

The main modeling unit is a business process, not a long-lived entity.

## Process streams

An event stream represents the execution of a business process. It should answer:

> How is this process currently executing?

It should not answer:

> What happened to this entity across its entire lifetime?

Use process-oriented stream names. Prefer gerund-style process names where they fit the domain language, with the gerund last.

Examples:

- `user_onboarding`
- `user_login_claiming`
- `user_alias_claiming`
- `campaign_configuring`
- `campaign_running`

Avoid entity-history streams such as:

- `user`
- `organization`
- `campaign`

The event model names each aggregate in snake_case, and the aggregate name is the stream name. The implementation has no aggregate classes: each aggregate is a stream, named in PascalCase (`user_onboarding` is `UserOnboarding-{id}`). The id in the stream name is the aggregate id: the `user_onboarding_id` for user onboarding, the `user_id` for user personal data and user logins, and the alias for user alias claiming. The onboarding stream carries the `user_id` of the person in `UserOnboardingInitiated`; the two ids are separate and may differ.

## User personal data record

`user_personal_data` is a deliberate exception to the process-stream rule. It is not a business process and it has no domain invariants: it is the single data-protection record that holds the only PII in the system.

The exception exists so that erasure is possible in one place. The record:

- is created at user onboarding initiation, in the same atomic multi-stream append as the user login claim and the onboarding process
- holds only identifying data (`name`, `last_name`, `email`, `is_email_verified`)
- appends `UserPersonalDataErasureRequested`, `UserPersonalDataErasureCanceled` and `UserPersonalDataErasureApplied` to model the right to be forgotten
- is truncated before `UserPersonalDataErasureRequested` when the erasure is applied, so the erasure events survive and the PII disappears

The erasure events are the only events that remain in the stream. Every read model that consumes `UserPersonalDataRecorded` must also consume `UserPersonalDataErasureApplied`; otherwise a projection rebuild would re-materialize PII.

User logins are pseudonymous and are not PII: the user login key is an HMAC and the claim stream is retained so the same provider account can sign up again. Erasure releases the user logins; it does not delete the claim history.

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

- the `user_personal_data` record
- the `user_login_linking` index
- the `user_login_claiming` stream
- the user onboarding process stream

Each stream must be written with the expected version returned by the load step. Cancelling onboarding writes the onboarding cancellation, the user login and alias releases, and the erasure request atomically.

## Idempotency

Idempotency should be modeled explicitly.

If a command observes that the intended business fact has already happened, the handler should return the existing result and append no new events.

Tests should assert this by comparing stream versions before and after the request.

## Domain events

Domain events describe facts that already happened.

Prefer names such as:

- `UserOnboardingInitiated`
- `UserOnboardingAliasChosen`
- `UserLoginClaimed`
- `UserPersonalDataErasureApplied`

Avoid technical or CRUD-style names such as:

- `UserCreated`
- `DatabaseRecordInserted`
- `StatusUpdated`

Event schemas must remain backward compatible or have an explicit migration strategy.

Persisted event names and serialization are defined in [Slicent](slicent.md#event-serialization).
