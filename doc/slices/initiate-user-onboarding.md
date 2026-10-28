# Initiate user onboarding

## Purpose

Initiate the user onboarding process for an authenticated external identity.

The slice creates a user onboarding process when the external identity is not already known. If the same trusted external identity retries, the slice returns the existing onboarding id and appends no new events.

## Endpoint

`POST /user-onboarding/initiate`

The request body is currently empty. Identity and personal details come from the authenticated principal.

## Inputs

The slice currently depends on Google-style JWT claims:

- `sub`
- `iss`
- `given_name`
- `family_name`
- `email`
- `email_verified`

The JWT stays intact. The mapper reads personal details from `ClaimsPrincipal` and reads external identity through the registered external identity retriever.

This provider-specific dependency is intentional for this slice because initiating onboarding is the boundary where an external OpenID Connect identity becomes a Tooyioo onboarding process. Other slices should rely on app-owned principal claims such as `user_onboarding_id` or `user_id` rather than Google-specific claims.

## Streams and events

When the external identity is unknown, the slice writes two process streams atomically:

- The user onboarding stream receives `UserOnboardingInitiated`, which contains the personal details, email verification value, and external identity details.
- The external identity claiming stream receives `ExternalIdentityClaimed`.

## Idempotency

The external identity claiming stream is checked first.

If it already contains a user onboarding id, the command is treated as an idempotent retry by the same trusted external principal:

- return `200 OK`
- return the existing user onboarding id
- append no new events

Tests should assert this by capturing stream snapshots in `Given()` and using `ShouldHaveNoChangesSince(...)` in `Then`.

## Consistency

Initiating onboarding must claim the external identity and create the onboarding process as one business decision.

The handler writes both streams with expected stream versions. If another request claims the same external identity between load and append, Eventuous raises an optimistic concurrency exception and the slice returns a conflict result.

## Decisions

- Personal details are parsed from the JWT claims principal, not from a separate side-channel service.
- The external identity claim is its own process stream so uniqueness is protected independently from the onboarding process stream.
- Idempotency is based on the trusted external identity, not on request body content.
- Google is the initial OpenID Connect provider for simplicity. The broader auth model should remain provider-agnostic by keeping provider-specific parsing at the auth/onboarding boundary and using app-owned claims elsewhere.
- Slice tests use real HTTP, real auth middleware with a test signing key, and an in-memory event store.

## Open questions

- Whether unverified emails should still be allowed to initiate onboarding. The current event records `IsEmailVerified` as `false` when the provider says the email is unverified.
- Whether provider-specific claim normalization should stay in the slice or move behind provider-specific infrastructure if more identity providers are added.
