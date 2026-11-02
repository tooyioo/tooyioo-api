# Initiate user onboarding

- **Event model:** [01-user_onboarding.em.hcl](../event-model/01-user_onboarding.em.hcl)
- **Workflow:** `initiate_user_onboarding`
- **Tests:** [InitiateUserOnboarding tests](../../test/Tooyioo.Tests/Features/InitiateUserOnboarding)

## Decisions

- The command mapper mints two ids at initiation: `user_onboarding_id` for the onboarding process/stream and `user_id` for the person. They may share a value, but they are separate concepts.
- `UserOnboardingInitiated` records the `user_id` once. The onboarding stream is keyed by `user_onboarding_id`. PII is written only to the `user_personal_data` stream.
- The user login is identified by `HMAC(pepper, kind|issuer|subject)`, not by the raw provider `sub`. The issuer comes from the validated token and is matched exactly by the JWT middleware; changing it re-keys existing user logins.
- The user login claim is the idempotency key: a retry for an already claimed user login returns the existing `user_id` and appends nothing. A released user login can be claimed again and mints a new `user_id`.
- Initiation is one atomic multi-stream write: `user_personal_data`, `user_login_linking`, `user_login_claiming`, and `user_onboarding`.

## Constraints

- Auth: the endpoint accepts an external identity (`iss` and `sub`) and the personal-detail claims; it does not require a `user_id` claim yet.
- Multi-stream consistency: the four streams are appended with expected versions. A concurrent claim returns a conflict.
- Provider evolution: adding a provider is a JWT configuration plus a user login link. Switching from Google direct to an IdP changes the user login key and needs a one-time link migration; the `user_id` does not change.

## Open questions

- Whether unverified emails should be allowed to initiate onboarding.
- When multi-login linking arrives, whether a user can link a second provider before completing onboarding.
