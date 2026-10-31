# Complete user onboarding

- **Event model:** [10-complete_user_onboarding.em.hcl](../event-model/10-complete_user_onboarding.em.hcl)
- **Tests:** [CompleteUserOnboarding tests](../../test/Tooyioo.Tests/Features/CompleteUserOnboarding)

## Decisions

- Completion writes only to the user onboarding process stream.
- Completion requires a previously chosen alias; the handler does not choose or generate one.
- Completion is idempotent once `UserOnboardingCompleted` has been recorded: retries return the existing user id.
- Completed external identities may still receive `user_onboarding_id` in the principal so idempotent onboarding endpoints can authorize retries.

## Constraints

- Auth: the authenticated principal must contain the matching app-owned `user_onboarding_id` claim.
- Single-stream consistency: completion appends with the expected stream version. No multi-stream write is involved.
