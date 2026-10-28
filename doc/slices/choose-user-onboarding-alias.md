# Choose user onboarding alias

## Purpose

Claim an available alias and assign it to an existing user onboarding.

## Endpoint

`POST /user-onboarding/{id}/choose-alias`

The authenticated principal must contain the matching app-owned `user_onboarding_id` claim.

## Rules

- An alias is globally unique while its claiming stream records an active claim.
- Choosing the same alias again before completion is idempotent and appends no new events.
- A different alias cannot replace an alias already chosen for the onboarding.
- Only an in-progress onboarding can choose an alias. A canceled or completed onboarding returns `onboarding_not_in_progress`.

## Streams and events

When an alias is available and the onboarding is in progress, the slice appends these events atomically:

- `UserOnboardingAliasChosen` to the user onboarding stream.
- `AliasClaimed` to the alias claiming stream.

If another onboarding already owns the alias, the command returns `alias_already_claimed` and appends no events.

## Consistency

The command reads the user onboarding state and the alias claiming state. It appends both changes with expected stream versions so the onboarding assignment and global alias claim are one business decision.
