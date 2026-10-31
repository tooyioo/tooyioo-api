# Initiate user onboarding

- **Event model:** [01-user_onboarding.em.hcl](../event-model/01-user_onboarding.em.hcl)
- **Workflow:** `initiate_user_onboarding`
- **Tests:** [InitiateUserOnboarding tests](../../test/Tooyioo.Tests/Features/InitiateUserOnboarding)

## Decisions

- Personal details are parsed from the JWT claims principal, not from a separate side-channel service.
- The external identity claim is its own process stream so uniqueness is protected independently from the onboarding process stream.
- Idempotency is based on the trusted external identity, not on request body content.
- Google is the initial OpenID Connect provider. Provider-specific parsing stays at the auth/onboarding boundary; other slices use app-owned claims such as `user_onboarding_id` and `user_id`.
- Slice tests use real HTTP, real auth middleware with a test signing key, and an in-memory event store.

## Constraints

- Auth: Google-style JWT claims (`sub`, `iss`, `given_name`, `family_name`, `email`, `email_verified`).
- Multi-stream consistency: onboarding creation and external identity claim are one expected-version write. A concurrent claim returns a conflict.
- Idempotency: a retry for an already claimed external identity returns the existing onboarding id and appends no events.

## Open questions

- Whether unverified emails should be allowed to initiate onboarding.
- Whether provider-specific claim normalization should move behind provider-specific infrastructure if more identity providers are added.
