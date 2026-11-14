# User personal data erasure

- **Event model:** [05-user_personal_data.em.hcl](../event-model/05-user_personal_data.em.hcl)
- **Workflows:** `retrieve_user_personal_data`, `request_user_personal_data_erasure`, `cancel_user_personal_data_erasure`, `retrieve_pending_user_personal_data_erasures`, `purge_user_personal_data_erasures`, `apply_user_personal_data_erasures`
- **Tests:** [UserPersonalData tests](../../test/Tooyioo.Tests/Features/UserPersonalData)

## Decisions

- **PII and process are separate streams.** `user_personal_data/{user_id}` holds only `UserPersonalDataRecorded`; `user_personal_data_erasure/{user_id}` holds the request, the cancellation, the applying marker and the applied fact. The erasure process holds no PII and is never truncated, so it survives the erasure and is the source of truth for the status.
- **No grace period.** A request only marks the person for deletion. Nothing is erased until an operator starts a purge, and the request can be canceled until the purge starts applying it. The deletion is a manual, auditable operation, not a timer.
- **The purge snapshots what it promises.** The run event stores the same `user_ids` the endpoint returns, so the promise and the work cannot drift. A user who requests erasure after the snapshot is left for the next run; a user who cancels before the snapshot is excluded; a user who cancels after it is skipped when applied.
- **The run stream is the audit and the progress.** `PersonalDataErasureRunStarted` records who started the purge and the snapshot; `PersonalDataErasureRunUserErased` is appended atomically with `UserPersonalDataErasureApplied`, so progress cannot drift from the erasure. There is no separate completion event.
- **Release before truncate, applied last.** The apply releases every linked user login, truncates the personal data stream at its end and only then appends `UserPersonalDataErasureApplied`. If a step fails, the user stays pending and the next run retries it; releasing first means the login is free even if the truncate failed, so a failed erasure never blocks a re-registration.
- **`UserPersonalDataErasureApplying` is the cancellation barrier.** It is the point of no return: a cancel after it is rejected, so a canceled request can never have a truncated stream.
- **`InitiateUserOnboarding` is the safety net.** A user login claimed by an erased user is released and re-claimed for a new `user_id`. A missed release therefore never locks a person out, which is what allows the release list to come from the login index instead of a stricter mechanism.
- **Truncate, not delete.** The stream name stays valid and the erasure facts live in the erasure stream. A re-registration mints a new `user_id`, so the truncated stream is never reused.
- Read models may copy PII for search, but every projection that consumes `UserPersonalDataRecorded` must also consume `UserPersonalDataErasureApplied` and anonymize the copy. Otherwise a projection rebuild would re-materialize PII.
- User login claims are retained as pseudonymous anti-abuse records. Erasure releases them; a re-registration mints a new `user_id`.

## Constraints

- Auth: the self-service endpoints require the matching app-owned `user_id` claim. The purge endpoint is modeled with an `administrator` actor; the authorization mechanism is not implemented yet.
- The apply is two atomic appends (applying plus releases, then applied plus run progress) separated by a stream truncation. The truncate is not part of either append, which is why `Applied` is written last.
- Scavenging is an operational step, not application code. Backups retain the truncated events until they rotate.
- Every PII-consuming projection must consume `UserPersonalDataErasureApplied`.

## Open questions

- How the administrator actor is authenticated and authorized.
- Whether the run status is exposed over HTTP or read from the run stream by an operator tool.
- Whether a run can be resumed from its own stream instead of from the pending read model.
