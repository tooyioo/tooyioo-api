# Cancel user onboarding

- **Event model:** [02-user_onboarding_cancellation.em.hcl](../event-model/02-user_onboarding_cancellation.em.hcl)
- **Workflow:** `cancel_user_onboarding`
- **Tests:** [CancelUserOnboarding tests](../../test/Tooyioo.Tests/Features/CancelUserOnboarding)

## Decisions

- Cancellation is terminal and immediately requests the personal data erasure: the erasure request is appended atomically with the cancellation, so a canceled onboarding never exists without a pending or applied erasure.
- The user login and the alias (when one exists) are released in the same atomic append. Releasing the user login is what allows the same provider account to sign up again.
- The cancellation reason is a bounded reason code, not free text. Free text could contain PII and would be written to a stream that is retained.
- The public endpoint is not idempotent after a successful cancellation because the released user login no longer resolves the user. The handler is still defensively idempotent if invoked directly with an already canceled stream.
- The erasure is applied by the scheduled processor, not by the cancel command. This keeps the command free of destructive side effects and makes the erasure retryable.

## Constraints

- Auth: the route carries the `user_onboarding_id`; the authorizer loads the onboarding state and checks its `user_id` against the app-owned `user_id` claim. There is no `user_onboarding_id` claim.
- Multi-stream consistency: the cancellation, the user login and alias releases, and the erasure request are one expected-version write. A concurrent change returns a conflict.
