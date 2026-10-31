# Choose user onboarding alias

- **Event model:** [01-user_onboarding.em.hcl](../event-model/01-user_onboarding.em.hcl)
- **Workflow:** `choose_user_onboarding_alias`
- **Tests:** [ChooseUserOnboardingAlias tests](../../test/Tooyioo.Tests/Features/ChooseUserOnboardingAlias)

## Decisions

- Alias uniqueness is protected by a separate claiming stream, not by scanning onboardings.
- The alias claim represents current availability. Releasing an alias makes it claimable again.
- Choosing the same alias again before a terminal state is idempotent. A different alias cannot replace one already chosen.
- The onboarding assignment and the global alias claim are one business decision.

## Constraints

- Auth: the authenticated principal must contain the matching app-owned `user_onboarding_id` claim.
- Multi-stream consistency: `UserOnboardingAliasChosen` and `AliasClaimed` are appended with expected versions. A concurrent claim conflict returns a conflict result.
