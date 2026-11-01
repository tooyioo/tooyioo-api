# Choose user onboarding alias

- **Event model:** [01-user_onboarding.em.hcl](../event-model/01-user_onboarding.em.hcl)
- **Workflow:** `choose_user_onboarding_alias`
- **Tests:** [ChooseUserOnboardingAlias tests](../../test/Tooyioo.Tests/Features/ChooseUserOnboardingAlias)

## Decisions

- Alias uniqueness is protected by a separate alias claim stream, not by scanning user onboardings.
- A claim means the alias is currently reserved by one user onboarding. It does not mean the alias existed historically. Releasing an alias makes it claimable again.
- Choosing the same alias again before a terminal state is idempotent. A different alias cannot replace the one already chosen.
- The alias assigned to the user onboarding and the global alias claim are one business decision.

## Constraints

- Auth: the authenticated principal must contain the matching app-owned `user_onboarding_id` claim.
- Multi-stream consistency: `UserOnboardingAliasChosen` and `AliasClaimed` are appended with expected versions. A concurrent claim conflict returns a conflict result.
