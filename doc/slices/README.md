# Slice decision records

A slice decision record is not a second specification. It captures only knowledge that cannot be derived from the HCL event model, the tests, or the code.

## Where things live

See [doc/index.md](../index.md) for artifact ownership and primary audiences. In short: HCL is behavior, tests are acceptance, code is implementation, and this folder holds decisions.

## When to add or update a slice decision record

Add one when the slice has knowledge that is not obvious from the HCL or the code:

- auth or identity assumptions
- idempotency semantics
- optimistic concurrency or conflict behavior
- multi-stream atomicity decisions
- read-model projection, indexing, pagination, or consistency decisions
- external provider or integration assumptions
- rejected alternatives that a future maintainer may reintroduce
- explicit product assumptions or open questions
- cross-slice contracts that are not visible in one workflow

Do not add one only because a slice exists.

Do not include:

- Purpose, endpoint, inputs, streams, events, fields, business rules, or scenarios that the HCL already expresses
- a list of class names or test names
- implementation details that the code already makes obvious
- a second copy of the current behavior

> If a section can be derived by reading the HCL workflow, the tests, or the code, delete it.

## Location and header

Use:

```text
doc/slices/<slice-name>.md
```

Start with links to the chapter model, workflow ID, and tests:

```markdown
# Complete user onboarding

- **Event model:** `../event-model/<chapter>.em.hcl`
- **Workflow:** `<workflow_id>`
- **Tests:** `../../test/Tooyioo.Tests/Features/<Feature>`
```

If the slice is not yet modeled, say so explicitly:

```markdown
- **Event model:** Not yet modeled in `doc/event-model`.
```

## Template

```markdown
# Slice name

- **Event model:** `../event-model/<chapter>.em.hcl`
- **Workflow:** `<workflow_id>`
- **Tests:** `../../test/Tooyioo.Tests/Features/<Feature>`

## Decisions

- Non-obvious choices that should survive refactoring.
- Rejected alternatives, if a future maintainer could reintroduce them.

## Constraints

- Auth: required claims, policies and ownership rules.
- Idempotency: what happens when the command or query repeats.
- Consistency: expected stream version, conflict behavior.
- Multi-stream: which streams are written atomically, if any.
- Read model: projection, indexing, pagination, visibility or lag constraints.

## Open questions

- Known gaps that must not be guessed.
```

Omit empty sections. A short record with two decisions is better than a long record that restates the model.

## Keeping it aligned

Change the artifact that owns the knowledge. Normative agent rules are in [AGENTS.md](../../AGENTS.md).

- Behavior or acceptance criteria: HCL first, then tests and code.
- Non-obvious decision or constraint: this record, then code/tests if affected.
- Implementation detail: code, with tests if behavior is affected.
- Public setup or published artifacts: `README.md`.

When you touch a slice decision record, remove restatements that have drifted. Scenario traceability is covered in [Testing](../guides/testing.md#scenario-traceability).
