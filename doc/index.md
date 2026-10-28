# Tooyioo documentation index

This folder contains project knowledge that agents should use before changing code.

Use this index to choose the smallest relevant reading set for a task.

## Core docs

- [Architecture](architecture.md) explains the system shape, module boundaries, and where code belongs.
- [Event sourcing](event-sourcing.md) explains the aggregate-less process-stream model.
- [Vertical slice template](vertical-slice-template.md) explains how new slices should be structured.
- [Testing](testing.md) explains TUnit vertical slice tests and the reusable test host.
- [Auth](auth.md) explains identity, JWT claims, and app-owned claims.
- [Slicent](slicent.md) explains how to work on the local framework.

## Slice docs

- [Slice decision records](slice-decision-records.md) explains when to add per-slice specs or decision notes.
- [Initiate user onboarding](slices/initiate-user-onboarding.md) records current behavior and decisions for the onboarding initiation slice.
- [Complete user onboarding](slices/complete-user-onboarding.md) records current behavior and decisions for the onboarding completion slice.
- [Cancel user onboarding](slices/cancel-user-onboarding.md) records current behavior and decisions for the onboarding cancellation slice.
- [Choose user onboarding alias](slices/choose-user-onboarding-alias.md) records the alias selection rules and consistency boundary.

## Agent workflow

Before implementing, read:

1. `AGENTS.md`.
2. This index.
3. The docs relevant to the change.
4. Existing slices or tests that are closest to the requested work.

Do not invent missing business rules. If a behavior is unclear, ask for clarification or document the assumption explicitly.
