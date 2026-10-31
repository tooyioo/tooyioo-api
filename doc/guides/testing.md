# Testing

Application vertical slice tests belong in:

- `test/Tooyioo.Tests`

Slicent framework tests belong in:

- `test/Slicent.Tests`

## Vertical slice test intent

Vertical slice tests should exercise the real behavior path:

- ASP.NET Core endpoint
- authorization policy
- mapper
- command or query dispatcher
- handler
- Eventuous persistence boundary
- response mapper

They should avoid production infrastructure unless the infrastructure itself is the behavior under test.

## Event store and read model

`Host.Given` writes events to the in-memory event store. It does **not** run subscriptions or projectors; the read model stays absent unless the test projects it.

`Host.GivenAndProject<TProjector>` writes the events and explicitly runs the projector.

Auth in the test host resolves app-owned claims from the event store. A query test that needs `user_id` or `user_onboarding_id` must set up the corresponding events even when it intentionally leaves the read model absent. With no events, the request is rejected with `403` before the query can return `404`.

## Scenario traceability

Tests are the executable acceptance criteria for the HCL event model. Each named HCL scenario that matters should have a corresponding test.

When the mapping between a test class and an HCL scenario is not obvious, add a comment above the class:

```csharp
// HCL scenario: complete_user_onboarding/scenario_02_user_onboarding_without_alias_cannot_be_completed
```

Do not maintain a separate scenario-to-test table; it will drift. The HCL workflow, the test, and the comment are enough.

## Test host

Use `VerticalSliceTestHost` for application slice tests.

The host uses the real API `Program.cs` through `WebApplicationFactory`, then overrides only the services that make tests slow or external:

- event store, reader, and writer use `Eventuous.Testing.InMemoryEventStore`
- MongoDB is replaced enough for service registration to validate
- hosted background services are removed
- JWT bearer validation is replaced with a test signing key

The test factory sets `Slicent:EventStoreProvider` to `InMemory` in host configuration before `Program` executes. This avoids registering the KurrentDB client and hosted read-model subscription. MongoDB remains independently configured: query tests provide a real test database, while command tests do not use it. The `Testing` environment alone does not change the default `KurrentDb` event-store provider, so it remains available for local runs that use real Aspire infrastructure.

This keeps tests close to production wiring without starting Aspire, KurrentDB, MongoDB containers, or the real Google issuer metadata flow.

## Vertical slice test bases

Use the intention-level base class for the kind of slice under test:

- `CommandVerticalSliceTest` for command/write-side vertical tests.
- `QueryVerticalSliceTest` for Mongo-backed query/read-model vertical tests.

Both bases use the same Given/When/Then lifecycle internally:

- creates a fresh host per test
- runs `Given()`
- runs `When()`
- disposes the host after the test

Each `[Test]` method should be a single `Then` assertion or a closely related assertion group.

Example shape:

```csharp
public sealed class WhenGoogleIdentityIsUnknown : CommandVerticalSliceTest
{
    protected override Task Given()
    {
        return Task.CompletedTask;
    }

    protected override async Task When()
    {
        // compose token, send HTTP request, read response
    }

    [Test]
    public async Task Then_response_is_ok()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.OK);
}
```

## Auth in tests

Use `GoogleIdentityTokenBuilder` to create signed JWTs.

The test JWT is real and signed, but tests do not validate Google issuer metadata or production audience. The test setup keeps signing key and lifetime validation enabled.

JWT claims should stay honest. If the slice needs personal details from an identity token, read them from `ClaimsPrincipal`; do not create side-channel test-only services for the same data.

## Event assertions

Use `Host.Events.Stream<TState, TId>(id)` to assert stream behavior.

Use:

- `ShouldContainExactly(...)` when asserting a newly written stream.
- `ShouldContain<TEvent>()` when event order or exact stream contents are not the test concern.
- `ShouldNotContain<TEvent>()` when a specific event must not appear.
- `CaptureSnapshot()` in `Given()` and `ShouldHaveNoChangesSince(snapshot)` in `Then` when asserting idempotency.

`ShouldHaveNoChangesSince` compares the Eventuous stream version. It also checks the captured event count and values to improve diagnostics.

## Deterministic identifiers

Use the test GUID extension for stable identifiers:

```csharp
var id = 1.ToGuid();
```

This avoids unexplained magic GUID strings while keeping tests deterministic.

## Parallel execution

Slice tests must be isolated.

Each test gets:

- a fresh `VerticalSliceTestHost`
- a fresh in-memory event store
- its own HTTP client

Do not share mutable test host state across test classes.

## MongoDB read-model tests

Read-model tests that need MongoDB should inherit from `QueryVerticalSliceTest`.

Those tests use:

- one shared MongoDB Testcontainer for the test assembly, using `mongo:8.3`
- one unique Mongo database per `[Test]` execution
- explicit projector execution during `Given()`
- database cleanup when the per-test host is disposed

This keeps the existing Given/When/Then autonomy: every `Then` still gets a fresh host, event store, HTTP client, and Mongo database. Mongo-backed read-model tests are parallel-limited so one container is not overloaded by unbounded concurrent databases, projections, queries, and database drops.

Docker is required to run these tests. If Docker is unavailable on the machine running the test suite, Mongo-backed read-model tests are dynamically skipped instead of failing the full application test run.
