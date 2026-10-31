# Cancel user onboarding

- **Event model:** [02-user_onboarding_cancellation.em.hcl](../event-model/02-user_onboarding_cancellation.em.hcl)
- **Workflow:** `cancel_user_onboarding`
- **Tests:** [CancelUserOnboarding tests](../../test/Tooyioo.Tests/Features/CancelUserOnboarding)

## Decisions

- Claiming streams represent current availability, not historical existence. A claim followed by a release makes the external identity or alias available for reuse.
- Cancellation is terminal. Once the external identity claim is released, public onboarding endpoints can no longer resolve a `user_onboarding_id` claim for the canceled onboarding.
- The cancellation reason is stored on the onboarding process stream. Release events only record the owning user onboarding id.
- The alias claim is released only when the onboarding actually has an alias.

## Constraints

- Auth: the authenticated principal must contain the matching app-owned `user_onboarding_id` claim.
- Multi-stream consistency: cancellation and all owned claim releases are one expected-version write. Any concurrent change returns a conflict.
- Idempotency: the public endpoint is not idempotent after a successful cancellation because the claims are released. The handler still treats an already-canceled stream as defensively idempotent if invoked directly.
