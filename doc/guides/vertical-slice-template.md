# Vertical slice template

A vertical slice should contain the code required to complete one user-meaningful behavior.

Prefer following existing slices before introducing a new shape.

## Typical slice structure

For command slices, expect:

- request contract
- response contract
- endpoint
- mapper from HTTP/request/auth context to command
- command
- command handler
- state used for decisions
- domain events
- authorizer, when required
- support types that are private to the slice
- vertical slice tests

For query slices, expect:

- route/request contract
- response contract
- endpoint
- mapper
- query
- query handler
- read model document or projection support
- authorizer, when required
- vertical slice tests or focused query tests

## Contracts

Contracts that are part of the module's public integration surface belong in the module contract project.

Keep contracts small and explicit. Do not leak internal state objects, Eventuous types, or persistence details through HTTP contracts.

## Endpoints and mappers

Endpoints should stay thin.

Mappers translate transport concerns into application commands or queries. Reading `ClaimsPrincipal` is acceptable in mappers when the slice depends on authenticated identity.

Command handlers should not know about HTTP.

## Handlers

Command handlers own business decisions.

They should:

- load the process state needed for the decision
- evaluate invariants
- choose domain events
- store events with expected stream versions
- return an application result

They should not:

- perform HTTP-specific logic
- update read models directly
- call unrelated slices as service layers
- hide business decisions in infrastructure services

## State

State classes fold domain events and expose only what the handler needs for decisions.

Avoid turning state into a general entity model.

## Documentation

Every slice should link to its chapter HCL file, name its workflow ID, and link its tests. Add the workflow block to the chapter's `.em.hcl` file; do not create a per-workflow file. Add a [slice decision record](../slices/README.md) only when the slice has non-obvious decisions, constraints, external assumptions, or open questions; do not restate HCL behavior.

## Tests

Use [Testing](testing.md) for test bases, auth, event assertions, and scenario traceability. Each named HCL scenario that matters should have a test.

## Common mistakes

- Placing business rules in endpoint code.
- Creating generic services before a second slice proves the need.
- Modeling long-lived entity history instead of a process.
- Testing only the HTTP response while ignoring persisted events.
- Duplicating production infrastructure in slice tests when an in-memory store would prove the same behavior faster.
