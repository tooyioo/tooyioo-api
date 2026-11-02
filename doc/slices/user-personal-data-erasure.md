# User personal data erasure

- **Event model:** [05-user_personal_data.em.hcl](../event-model/05-user_personal_data.em.hcl)
- **Workflows:** `retrieve_user_personal_data`, `request_user_personal_data_erasure`, `cancel_user_personal_data_erasure`, `apply_due_user_personal_data_erasures`
- **Tests:** [UserPersonalData tests](../../test/Tooyioo.Tests/Features/UserPersonalData)

## Decisions

- The `user_personal_data` stream is the only place that stores PII. It is a data-protection record, not a business process, and it is the deliberate exception to the process-stream rule.
- The PII slices live in `Tooyioo.User`, together with onboarding, authentication and the directory. The events live in `Tooyioo.User.Contracts`; the state and slices live in the implementation. The initiation, cancellation and erasure handlers write the streams directly in the same module.
- Erasure is modeled on the record: `UserPersonalDataErasureRequested`, `UserPersonalDataErasureCanceled` and `UserPersonalDataErasureApplied`. The request carries a due date so a policy change does not alter existing requests.
- Canceling onboarding requests an immediate erasure. A user-initiated erasure uses the configured grace period (14 days by default) and can be canceled during it.
- Applying the erasure appends `UserPersonalDataErasureApplied`, releases every user login from the `user_login_linking` index, and then truncates the stream before the `UserPersonalDataErasureRequested` event. The request and applied events survive; the PII disappears.
- The stream is truncated instead of deleted. A tombstone would make the stream name permanently unusable and would prevent a later re-registration; truncation keeps the erasure facts and the identity reusable.
- The scheduled processor polls the `user_personal_data` read model for due requests and is idempotent. If the truncate fails after the append, the state is already erased and the retry truncates again.
- Read models may copy PII for search, but every projection that consumes `UserPersonalDataRecorded` must also consume `UserPersonalDataErasureApplied` and anonymize the copy. Otherwise a projection rebuild would re-materialize PII.
- User login claims are retained as pseudonymous anti-abuse records. Erasure releases them; a re-registration mints a new `user_id`.

## Constraints

- Auth: every endpoint requires the matching app-owned `user_id` claim.
- The apply write is a multi-stream append (personal data, user login index and each claimed user login) followed by a stream truncation. The truncate is not part of the atomic append.
- Scavenging is an operational step, not application code. Backups retain the truncated events until they rotate.
- Every PII-consuming projection must consume `UserPersonalDataErasureApplied`.

## Open questions

- Whether the grace period should be shorter than the current 14 days.
- Whether an operator-initiated erasure endpoint is required in addition to the authenticated self-service one.
- Whether the erased user document should be hidden from the directory entirely instead of returned masked.
