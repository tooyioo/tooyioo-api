# Complete user onboarding

## Purpose

Complete an existing user onboarding process after the user has chosen an alias and accepted a terms and conditions version.

The slice appends `UserOnboardingCompleted` to the user onboarding process stream and returns the generated Tooyioo user id.

## Endpoint

`POST /user-onboarding/{id}/complete`

The route id is the user onboarding id. The authenticated principal must contain the matching app-owned `user_onboarding_id` claim.

## Inputs

The request body contains:

- `termsAndConditionsVersion`, required

The endpoint does not depend on Google-specific claims directly. Provider-specific identity is resolved by authentication infrastructure, which enriches the principal with app-owned claims.

## Business rules

- The onboarding must exist.
- A canceled onboarding cannot be completed.
- An alias must have been chosen before the onboarding can be completed. Otherwise the command returns `onboarding_alias_not_chosen` and appends no events.

## Streams and events

The user onboarding stream receives:

- `UserOnboardingCompleted`

The event contains:

- generated `UserId`
- accepted `TermsAndConditionsVersion`

## Idempotency

If the user onboarding stream is already completed, the command returns the existing user id and appends no new events.

This depends on `UserOnboardingState` applying `UserOnboardingCompleted` and remembering the completed `UserId`.

## Consistency

Completing onboarding appends to a single process stream with the expected stream version from the loaded state.

If another request appends to the stream between load and append, Eventuous raises an optimistic concurrency exception and the slice returns a conflict result.

## Decisions

- Completion writes only to the user onboarding process stream.
- Completion requires a previously chosen alias; the handler does not choose or generate an alias.
- Completion is idempotent once `UserOnboardingCompleted` has been recorded.
- Completed external identities may still receive `user_onboarding_id` in the principal so idempotent onboarding endpoints can authorize retries.
