# Tooyioo - AI Agent Context

## Overview

This repository contains `tooyioo-api`, the backend for Tooyioo.

Tooyioo is a local demand-generation platform for small and medium businesses. It helps bars, restaurants, shops, and local merchants create campaigns and offers that can be distributed to nearby users, including through street promoters.

A typical use case is a hospitality business running a local promotion. Promoters approach potential customers, offer them a specific deal, and provide a finite numbered QR code that can be redeemed at the venue. This allows:

- merchants to attract demand when they need it
- promoters to keep operating in the real world while tracking attribution
- users to discover better local products, prices, and experiences
- businesses to measure which campaigns, offers, promoters, and audiences work best

The business model is subscription-based for merchants, with free usage options intended to increase market adoption.

---

# Architecture

Modular monolith on .NET 10 using Vertical Slice Architecture, CQRS, and aggregate-less Event Sourcing. Eventuous provides the event-sourcing primitives; Slicent provides local slice conventions.

See [Architecture overview](doc/architecture/overview.md), [Event sourcing](doc/architecture/event-sourcing.md), and [Slicent](doc/architecture/slicent.md). Prefer existing Slicent conventions before introducing new abstractions.

---

# Core Modeling Principle

Model business processes, not long-lived entities. Each process has one cohesive set of invariants, one consistency boundary, and one stream, and reconstructs only the state needed to decide the next command.

Do not add aggregate classes. The aggregate responsibilities are fulfilled by the stream, folded state, optimistic concurrency, and command handler decision logic.

See [Event sourcing](doc/architecture/event-sourcing.md).

---

# Repository Structure

The main repository folders are:

- `src/` - application and framework source code
- `test/` - automated tests
- `doc/` - documentation, specifications, diagrams, and design notes
- `bruno/` - Bruno collections to interact with the API

Main projects under `src/`:

- `Tooyioo.AppHost` - local development entry point using .NET Aspire
- `Tooyioo.Api` - main ASP.NET Core application, infrastructure, dependency wiring, and `Program.cs`
- `Tooyioo.Common` - shared code that is not generic framework code
- `Slicent` - reusable framework built around Eventuous, Vertical Slice Architecture, CQRS, and Event Sourcing

The application vertical slices are organised under their respective module folders.

---

# Infrastructure

Local development is orchestrated with .NET Aspire.

The main infrastructure components are:

- KurrentDB as the event store
- MongoDB as the document/read-model database
- OpenTelemetry for telemetry
- Aspire for local composition and observability

Do not introduce additional infrastructure dependencies without a clear reason and an accompanying design note.

---

# Development Principles

Prefer consistency with the existing codebase over introducing new patterns.

When implementing a feature:

1. Read this file and `doc/index.md`.
2. Read the relevant HCL workflow, its scenarios, and the slice decision record if one exists.
3. Inspect similar existing vertical slices and their tests.
4. If behavior changes, update the HCL event model first.
5. Follow the established conventions and keep the slice cohesive.
6. Add or update tests for the HCL scenarios.
7. Run `emhcl validate doc/event-model` and the relevant tests.
8. Update the slice decision record only if a non-obvious decision or constraint changed.

---

# Documentation and Source of Truth

Artifact ownership and primary audiences are defined in [doc/index.md](doc/index.md). In short: the HCL event model is behavior, tests are executable acceptance, code is implementation, and slice decision records capture decisions.

Rules:

- Do not restate HCL workflows, fields, events, scenarios, errors, or business rules in slice decision records.
- Every named HCL scenario that matters should have a test.
- Slice decision records exist only for knowledge that cannot be derived from HCL, tests, or code. Do not create one only because a slice exists.
- If code and HCL disagree, treat it as a defect. Resolve the disagreement; do not document around it.
- Change the artifact that owns the knowledge: behavior in HCL first; decisions in the slice decision record; implementation in code; setup in `README.md`.
- When a slice decision record stops being useful, delete the duplicated text instead of maintaining it.
- Link each slice decision record to its chapter HCL file, workflow ID, and test folder.

The detailed template and rules are in [doc/slices/README.md](doc/slices/README.md).

---

# Event Model Organization

The model lives in `doc/event-model`:

- It is a flat folder. EMHCL ignores subfolders, so do not put `.em.hcl` files in subfolders.
- `00-*` files are global: catalog, chapters, and hotspots.
- `NN-<chapter>.em.hcl` contains all workflows for chapter `NN`, in board order. The current convention is one file per chapter, not one file per workflow.
- File prefixes are for human navigation only. `board.workflows` and `chapter` define model order.
- When adding a workflow: add it to the board, add it to the chapter list, add the block to the chapter file, then add tests.
- When adding a chapter: add `chapter_NN_<name>`, create `NN-<chapter>.em.hcl`, and add its workflows to the board and chapter.
- Chapter IDs use `chapter_NN_`; scenario IDs use `scenario_NN_`, with happy paths before error scenarios.
- EMHCL 0.12.0+ must be on `PATH` on Windows and Linux; see [the event model README](doc/event-model/README.md).

See [Event model organization](doc/event-model/README.md#adding-a-chapter-or-workflow).

---

# Vertical Slices

Features should be implemented as vertical slices.

A vertical slice may contain:

- command definitions
- query definitions
- command handlers
- query handlers
- state projections
- domain events

Avoid spreading feature logic across generic service layers.

Avoid cross-slice coupling unless there is an explicit shared business concept.

---

# CQRS Rules

Commands mutate state. Queries read state. Query handlers must never mutate state.

The command handler is the transactional boundary: load process state, evaluate invariants, and append events with expected stream versions. Do not mix command-side and query-side responsibilities.

See [Event sourcing](doc/architecture/event-sourcing.md) for multi-stream and process-state details.

---

# Event Sourcing Rules

Follow [Event sourcing](doc/architecture/event-sourcing.md). Non-negotiable:

- Streams represent processes, not entity lifetimes. Use process-oriented names.
- State is short-lived decision state, not a read model or entity model.
- Each process owns one cohesive set of invariants and one consistency boundary.
- Events are immutable past-tense facts. Schema changes must stay backward compatible or include a migration strategy.

---

# Coding Standards

Use modern .NET 10 and C# conventions.

Prefer:

- explicit naming
- immutable data where practical
- small cohesive types
- clear command, query, and event names
- async/await end-to-end
- cancellation tokens on asynchronous operations where appropriate

Avoid:

- generic repository abstractions
- anemic domain models
- aggregate classes
- service-layer orchestration that bypasses slice boundaries
- hidden side effects
- blocking asynchronous calls
- unrelated refactoring

---

# Agent Workflow

Before making changes, agents should:

1. Read this file.
2. Read `doc/index.md`, the relevant HCL workflow and its scenarios, and the slice decision record if one exists.
3. Inspect similar implementations and their tests.
4. If behavior changes, update the HCL event model first.
5. Make the smallest coherent change in code and tests.
6. Run `emhcl validate doc/event-model` and the relevant tests.
7. Update the slice decision record only if a non-obvious decision or constraint changed.

Do not invent missing business rules.

If a business rule is unclear, ask for clarification rather than making assumptions.

Do not duplicate HCL behavior in slice decision records. Do not add a slice decision record only because a slice exists.
