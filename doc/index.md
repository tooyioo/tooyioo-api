# Tooyioo documentation index

This folder contains project knowledge that agents and maintainers should use before changing code.

Docs are grouped by purpose: `architecture/`, `guides/`, `slices/`, and `event-model/`.

Use this index to choose the smallest relevant reading set for a task.

## Source of truth

| Knowledge | Canonical location | Primary audience |
|---|---|---|
| Behavior, workflows, scenarios, errors, read models, fields, endpoint descriptions | `doc/event-model/*.em.hcl` | Product, domain, engineering, rendered diagram |
| Executable acceptance criteria | `test/Tooyioo.Tests/Features/**` | Engineering and CI |
| Implementation details: persistence, DI, Mongo queries, indexes, auth enrichment, framework wiring | `src/**` | Engineering |
| Non-obvious decisions, constraints, trade-offs, rejected alternatives, open questions | `doc/slices/<slice>.md` | Maintainers and agents |
| Architecture, coding rules, agent workflow | `AGENTS.md` and the docs in this folder | Agents and engineering |
| Human setup and published artifacts | `README.md` | New contributors and users |

Do not duplicate behavior in slice decision records. See [Slice decision records](slices/README.md) for the rules and template.

## Architecture

- [Overview](architecture/overview.md) explains the system shape, module boundaries, and where code belongs.
- [Event sourcing](architecture/event-sourcing.md) explains the aggregate-less process-stream model.
- [Auth](architecture/auth.md) explains identity, JWT claims, and app-owned claims.
- [Slicent](architecture/slicent.md) explains how to work on the local framework.

## Guides

- [Vertical slice template](guides/vertical-slice-template.md) explains how new slices should be structured.
- [Testing](guides/testing.md) explains TUnit vertical slice tests and the reusable test host.
- [Event model](event-model/README.md) explains the HCL model layout, local rendering, and Pages publishing.
- [Slice decision records](slices/README.md) explains what slice decision records own, what they must not duplicate, and how to keep them aligned.

## Slice decision records

- [Initiate user onboarding](slices/initiate-user-onboarding.md) records the identity, provider-boundary, and idempotency decisions for onboarding initiation.
- [Initiate business onboarding](slices/initiate-business-onboarding.md) records business onboarding decisions and open questions.
- [Complete user onboarding](slices/complete-user-onboarding.md) records completion decisions, including the alias prerequisite and idempotency behavior.
- [Cancel user onboarding](slices/cancel-user-onboarding.md) records terminal-state and claim-release decisions.
- [Choose user onboarding alias](slices/choose-user-onboarding-alias.md) records alias-claim availability and multi-stream consistency decisions.

## Agent workflow

Follow the workflow in [AGENTS.md](../AGENTS.md). In short: read this index, the relevant HCL workflow and scenarios, the slice decision record if one exists, and the closest existing slice and tests.
