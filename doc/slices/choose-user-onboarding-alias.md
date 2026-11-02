# Choose user onboarding alias

- **Event model:** [01-user_onboarding.em.hcl](../event-model/01-user_onboarding.em.hcl)
- **Workflow:** `choose_user_onboarding_alias`
- **Tests:** [ChooseUserOnboardingAlias tests](../../test/Tooyioo.Tests/Features/ChooseUserOnboardingAlias)

## Decisions

- Alias uniqueness is protected by a separate alias claim stream, not by scanning user onboardings.
- A claim means the alias is currently reserved by one user. It does not mean the alias existed historically. Releasing an alias makes it claimable again.
- Choosing the same alias again before a terminal state is idempotent. A different alias cannot replace the one already chosen.
- The alias assigned to the user onboarding and the global alias claim are one business decision.
- Alias format: 3-30 characters, lowercase letters, numbers, dashes and underscores. The alias claim is case-sensitive, so uppercase is rejected instead of normalized.

## Constraints

- Auth: the route carries the `user_onboarding_id`; the authorizer loads the onboarding state and checks its `user_id` against the app-owned `user_id` claim.
- Multi-stream consistency: `UserOnboardingAliasChosen` and `AliasClaimed` are appended with expected versions. A concurrent claim conflict returns a conflict result.
