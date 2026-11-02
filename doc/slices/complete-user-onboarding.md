# Complete user onboarding

- **Event model:** [01-user_onboarding.em.hcl](../event-model/01-user_onboarding.em.hcl)
- **Workflow:** `complete_user_onboarding`
- **Tests:** [CompleteUserOnboarding tests](../../test/Tooyioo.Tests/Features/CompleteUserOnboarding)

## Decisions

- Completion does not mint an id. The `user_onboarding_id` is the stream id and the `user_id` is the person id; both are minted at initiation, and `UserOnboardingInitiated` carries the `user_id`.
- Completion writes only to the user onboarding process stream.
- Completion requires a previously chosen alias; the handler does not choose or generate one.
- Terms acceptance stays on the onboarding process stream because consent evidence must survive an erasure. It is not stored in `user_personal_data`.
- Completion is idempotent once `UserOnboardingCompleted` has been recorded: retries return the same `user_id` and append no events.

## Constraints

- Auth: the authenticated principal must contain the matching app-owned `user_id` claim.
- Single-stream consistency: completion appends with the expected stream version. No multi-stream write is involved.
