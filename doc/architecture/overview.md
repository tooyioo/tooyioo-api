# Architecture

Tooyioo is a modular monolith targeting .NET 10.

The system uses:

- Vertical Slice Architecture
- CQRS
- aggregate-less Event Sourcing
- Eventuous for event sourcing primitives
- Slicent for local vertical slice conventions
- ASP.NET Core as the single deployable API host

## Module boundaries

Application behavior belongs in feature slices under module projects, for example:

- `src/user/Tooyioo.User` for user onboarding, authentication, personal data and the user directory
- `src/user/Tooyioo.User.Contracts` for the user events
- `src/business/Tooyioo.Business` for business onboarding
- `src/business/Tooyioo.Business.Contracts` for the business events

Modules only reference each other through their `*.Contracts`; feature slices stay inside their module. The `User` module contains the onboarding, authentication, personal-data and directory slices, and its initiation command writes all user streams in one append. `Business` is separate. The only implementation references are in the API composition root.

HTTP hosting, infrastructure wiring, authentication, OpenAPI, observability, and production service registration belong in:

- `src/Tooyioo.Api`

Reusable cross-module code that is specific to Tooyioo belongs in:

- `src/Tooyioo.Common`

Reusable framework code that should not know Tooyioo domain concepts belongs in:

- `src/Slicent`

## Slicent's role

Slicent provides conventions and wiring for vertical slices, commands, queries, contracts, and Eventuous integration.

Slicent should not contain Tooyioo business behavior. Prefer adding small convention helpers to Slicent only when more than one module needs the same framework-level pattern.

## Infrastructure

Local development is orchestrated by Aspire. Production-style infrastructure includes:

- KurrentDB for event storage
- MongoDB for read models
- OpenTelemetry for telemetry

Tests may replace infrastructure when the test intent is a slice-level behavior check. Do not introduce new infrastructure dependencies without a clear reason and a matching design note.

## Implementation rule

When adding behavior, place code where the behavior is owned:

- Endpoint, mapper, handler, state, events, and slice-specific support code stay in the slice.
- Shared business concepts go to the smallest appropriate shared module.
- Generic framework conventions go to Slicent.
- Host-level composition and external service integration stay in `Tooyioo.Api`.
