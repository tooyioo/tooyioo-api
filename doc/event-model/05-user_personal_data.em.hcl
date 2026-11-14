# Chapter 5 - User Personal Data and Erasure workflows, in board order.

state_view "retrieve_user_personal_data" {
  title       = "Retrieve User Personal Data"
  description = "GET /users/{user_id}/personal-data returns the authenticated user's personal data and erasure status."
  status      = "done"

  readmodel "user_personal_data" {
    title       = "User Personal Data"
    description = "Persisted User Personal Data read model. The only read model whose purpose is to expose PII to its owner. It joins the personal data stream with the erasure process stream and anonymizes on UserPersonalDataErasureApplied."
    question    = "Which personal data and erasure status does this user have?"
    from = [
      event.user.user_personal_data_recorded,
      event.user.user_personal_data_erasure_requested,
      event.user.user_personal_data_erasure_canceled,
      event.user.user_personal_data_erasure_applying,
      event.user.user_personal_data_erasure_applied,
    ]
    to = [screen.retrieve_user_personal_data_ui]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "name" {
      type     = "String"
      optional = true
      pii      = true
      example  = "Joe"
    }

    field "last_name" {
      type     = "String"
      optional = true
      pii      = true
      example  = "Bloggs"
    }

    field "email" {
      type     = "String"
      optional = true
      pii      = true
      example  = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "status" {
      type    = "String"
      example = "active"
    }

    field "created_at" {
      type    = "DateTime"
      example = "2026-11-01T00:00:00.000Z"
    }

    field "last_modified_at" {
      type    = "DateTime"
      example = "2026-11-01T00:00:00.000Z"
    }

    field "revision" {
      type    = "Long"
      example = 1
    }
  }

  screen "retrieve_user_personal_data_ui" {
    title = "User Personal Data"
    actor = actor.authenticated_user

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "name" {
      type     = "String"
      optional = true
      example  = "Joe"
    }

    field "last_name" {
      type     = "String"
      optional = true
      example  = "Bloggs"
    }

    field "email" {
      type     = "String"
      optional = true
      example  = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "status" {
      type    = "String"
      example = "active"
    }
  }

  scenario "scenario_01_active_user_personal_data_is_retrieved" {
    title = "Active personal data view"

    given {
      title = "The personal data was recorded"
      event = event.user.user_personal_data_recorded
    }

    then {
      title     = "The view shows the active personal data"
      readmodel = readmodel.user_personal_data
    }
  }

  scenario "scenario_02_erasure_requested_personal_data_is_retrieved" {
    title = "Pending erasure view"

    given {
      title = "The personal data was recorded"
      event = event.user.user_personal_data_recorded
    }

    given {
      title = "An erasure was requested"
      event = event.user.user_personal_data_erasure_requested
    }

    then {
      title     = "The view shows the pending erasure status"
      readmodel = readmodel.user_personal_data
    }
  }

  scenario "scenario_03_erased_user_personal_data_is_masked" {
    title = "Erased personal data view"

    given {
      title = "The personal data was recorded"
      event = event.user.user_personal_data_recorded
    }

    given {
      title = "The erasure was applied"
      event = event.user.user_personal_data_erasure_applied
    }

    then {
      title     = "The view shows a masked identity and the erased status"
      readmodel = readmodel.user_personal_data
    }
  }

  scenario "scenario_04_unknown_user_personal_data_is_not_found" {
    title = "No personal data exists for the requested user"

    given {
      title = "The personal data was not projected into the read model"
      event = event.user.user_personal_data_recorded
    }

    then {
      title = "The personal data cannot be retrieved"
      error = "user_personal_data_not_found"
    }
  }
}

state_change "request_user_personal_data_erasure" {
  title       = "Request User Personal Data Erasure"
  description = "PUT /users/{user_id}/personal-data/erasure marks the authenticated user's personal data for deletion. Nothing is erased until an operator runs the purge, and the request can be removed until the purge starts applying it."
  status      = "done"

  screen "request_user_personal_data_erasure_ui" {
    title = "Request User Personal Data Erasure"
    actor = actor.authenticated_user
    to    = [command.request_user_personal_data_erasure]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "reason_code" {
      type    = "String"
      example = "user_request"
    }
  }

  command "request_user_personal_data_erasure" {
    title       = "Request User Personal Data Erasure"
    description = "Appends UserPersonalDataErasureRequested to the erasure process stream. The personal data stream is untouched; the request holds no PII."
    aggregate   = aggregate.user.user_personal_data_erasure
    api_endpoint = "PUT /users/{user_id}/personal-data/erasure"

    aggregate_dependencies = [
      aggregate.user.user_personal_data,
    ]

    to = [
      event.user.user_personal_data_erasure_requested,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "reason_code" {
      type    = "String"
      example = "user_request"
    }
  }

  scenario "scenario_01_user_personal_data_erasure_is_requested" {
    title = "Request the erasure of active personal data"

    given {
      title = "The personal data was recorded"
      event = event.user.user_personal_data_recorded
    }

    when {
      title   = "Request the erasure"
      command = command.request_user_personal_data_erasure
    }

    then {
      title = "The erasure is requested"
      event = event.user.user_personal_data_erasure_requested
    }

    comment {
      description = "A repeated request while the erasure is pending returns the current status and appends nothing."
    }
  }

  scenario "scenario_02_erased_user_personal_data_cannot_be_erased_again" {
    title = "Request the erasure of already erased personal data"

    given {
      title = "The personal data was recorded"
      event = event.user.user_personal_data_recorded
    }

    given {
      title = "The erasure was applied"
      event = event.user.user_personal_data_erasure_applied
    }

    when {
      title   = "Request the erasure again"
      command = command.request_user_personal_data_erasure
    }

    then {
      title = "The personal data is already erased"
      error = "user_personal_data_already_erased"
    }
  }

  scenario "scenario_03_unknown_user_personal_data_cannot_be_erased" {
    title = "Request the erasure of unknown personal data"

    when {
      title   = "Request the erasure for an unknown user_id"
      command = command.request_user_personal_data_erasure
    }

    then {
      title = "No personal data exists for the given user"
      error = "user_personal_data_not_found"
    }
  }

  scenario "scenario_04_user_personal_data_erasure_is_requested_concurrently" {
    title = "Another request changes the erasure first"

    given {
      title = "The personal data was recorded"
      event = event.user.user_personal_data_recorded
    }

    when {
      title   = "Request the erasure while another request changes the erasure process"
      command = command.request_user_personal_data_erasure
    }

    then {
      title = "This attempt conflicts because the erasure process changed before the request was saved"
      error = "concurrent_update"
    }
  }
}

state_change "cancel_user_personal_data_erasure" {
  title       = "Cancel User Personal Data Erasure"
  description = "DELETE /users/{user_id}/personal-data/erasure removes a pending erasure. It is rejected once the purge has started applying it."
  status      = "done"

  screen "cancel_user_personal_data_erasure_ui" {
    title = "Cancel User Personal Data Erasure"
    actor = actor.authenticated_user
    to    = [command.cancel_user_personal_data_erasure]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }
  }

  command "cancel_user_personal_data_erasure" {
    title       = "Cancel User Personal Data Erasure"
    description = "Appends UserPersonalDataErasureCanceled to the erasure process stream. The personal data was never truncated."
    aggregate   = aggregate.user.user_personal_data_erasure
    api_endpoint = "DELETE /users/{user_id}/personal-data/erasure"

    to = [
      event.user.user_personal_data_erasure_canceled,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }
  }

  scenario "scenario_01_pending_erasure_is_canceled" {
    title = "Cancel a pending erasure"

    given {
      title = "An erasure was requested"
      event = event.user.user_personal_data_erasure_requested
    }

    when {
      title   = "Cancel the erasure"
      command = command.cancel_user_personal_data_erasure
    }

    then {
      title = "The erasure request is canceled"
      event = event.user.user_personal_data_erasure_canceled
    }
  }

  scenario "scenario_02_erasure_being_applied_cannot_be_canceled" {
    title = "Cancel an erasure that the purge is applying"

    given {
      title = "The erasure request is being applied"
      event = event.user.user_personal_data_erasure_applying
    }

    when {
      title   = "Cancel the erasure"
      command = command.cancel_user_personal_data_erasure
    }

    then {
      title = "The erasure can no longer be canceled"
      error = "erasure_already_applying"
    }
  }

  scenario "scenario_03_erased_user_personal_data_cannot_be_restored" {
    title = "Cancel the erasure of already erased personal data"

    given {
      title = "The erasure was applied"
      event = event.user.user_personal_data_erasure_applied
    }

    when {
      title   = "Cancel the erasure"
      command = command.cancel_user_personal_data_erasure
    }

    then {
      title = "The personal data is already erased"
      error = "user_personal_data_already_erased"
    }
  }

  scenario "scenario_04_erasure_that_was_never_requested_cannot_be_canceled" {
    title = "Cancel an erasure that was never requested"

    when {
      title   = "Cancel an erasure for an unknown or active user_id"
      command = command.cancel_user_personal_data_erasure
    }

    then {
      title = "There is no pending erasure to cancel"
      error = "erasure_not_requested"
    }
  }
}

state_view "retrieve_pending_user_personal_data_erasures" {
  title       = "Retrieve Pending User Personal Data Erasures"
  description = "Internal lookup; not an HTTP endpoint. The purge command reads it to snapshot the erasures it promises to apply."
  status      = "done"

  readmodel "pending_user_personal_data_erasures" {
    title       = "Pending User Personal Data Erasures"
    description = "View read by the purge command. It contains the erasure requests that have not been applied yet."
    question    = "Which personal data erasure requests are pending?"

    from = [
      event.user.user_personal_data_erasure_requested,
      event.user.user_personal_data_erasure_canceled,
      event.user.user_personal_data_erasure_applying,
      event.user.user_personal_data_erasure_applied,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "status" {
      type    = "String"
      example = "requested"
    }
  }
}

state_change "purge_user_personal_data_erasures" {
  title       = "Purge User Personal Data Erasures"
  description = "POST /admin/personal-data/erasure-runs starts a manual purge. It snapshots the pending erasures, records the run and returns the users it promised to erase. The erasures are applied asynchronously."
  status      = "done"

  screen "purge_user_personal_data_erasures_ui" {
    title = "Purge User Personal Data Erasures"
    actor = actor.administrator
    to    = [command.purge_user_personal_data_erasures]

    field "run_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-0000000000aa"
    }
  }

  command "purge_user_personal_data_erasures" {
    title       = "Purge User Personal Data Erasures"
    description = "Reads the pending erasures, mints a run_id, appends PersonalDataErasureRunStarted with the snapshot and returns the run id and the user_ids. It starts no work by itself. When nothing is pending it returns an empty snapshot and appends nothing."
    aggregate   = aggregate.user.personal_data_erasure_run
    api_endpoint = "POST /admin/personal-data/erasure-runs"

    to = [
      event.user.personal_data_erasure_run_started,
    ]

    field "run_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-0000000000aa"
    }

    field "requested_by" {
      type    = "String"
      example = "00000000-0000-0000-0000-0000000000aa"
    }

    field "user_ids" {
      cardinality = "List"
      type        = "String"
      example     = ["00000000-0000-0000-0000-000000000010"]
    }
  }

  scenario "scenario_01_pending_erasures_are_purged" {
    title = "A purge snapshots the pending erasures"

    given {
      title = "An erasure was requested"
      event = event.user.user_personal_data_erasure_requested
    }

    when {
      title   = "Start a purge"
      command = command.purge_user_personal_data_erasures
    }

    then {
      title = "The run records the snapshot of users to erase"
      event = event.user.personal_data_erasure_run_started
    }

    comment {
      description = "The response returns the same user_ids that the run event records, so the promise and the work cannot drift."
    }
  }

  scenario "scenario_02_canceled_erasures_are_not_purged" {
    title = "A canceled erasure is not part of the snapshot"

    given {
      title = "An erasure was requested"
      event = event.user.user_personal_data_erasure_requested
    }

    given {
      title = "The erasure was canceled"
      event = event.user.user_personal_data_erasure_canceled
    }

    when {
      title   = "Start a purge"
      command = command.purge_user_personal_data_erasures
    }

    then {
      title = "The canceled erasure is not in the run snapshot"
      event = event.user.personal_data_erasure_run_started
    }
  }
}

automation "apply_user_personal_data_erasures" {
  title       = "Apply User Personal Data Erasures"
  description = "Internal; not an HTTP endpoint. Started by PersonalDataErasureRunStarted. For each user in the run it sends ApplyUserPersonalDataErasure, which releases the user logins, truncates the personal data and marks the erasure as applied."
  status      = "done"

  readmodel "personal_data_erasure_run_progress" {
    title       = "Personal Data Erasure Run Progress"
    description = "Progress of a purge, reconstructed from its run stream. It is what tells the operator which users of the snapshot are already erased."
    question    = "Which users of this run are still pending?"

    from = [
      event.user.personal_data_erasure_run_started,
      event.user.personal_data_erasure_run_user_erased,
    ]

    field "run_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-0000000000aa"
    }

    field "user_ids" {
      cardinality = "List"
      type        = "String"
      example     = ["00000000-0000-0000-0000-000000000010"]
    }

    field "erased_user_ids" {
      cardinality = "List"
      type        = "String"
      example     = ["00000000-0000-0000-0000-000000000010"]
    }
  }

  command "apply_user_personal_data_erasure" {
    title       = "Apply User Personal Data Erasure"
    description = "Appends UserPersonalDataErasureApplying, appends every user login release in one atomic multi-stream append, truncates the personal data stream at its end and finally appends UserPersonalDataErasureApplied together with the run record. Applied is written last, so it means the releases and the truncate are both done. A failed step leaves the user pending and the next run retries it."
    aggregate   = aggregate.user.user_personal_data_erasure
    external_trigger = true

    aggregate_dependencies = [
      aggregate.user.user_personal_data,
      aggregate.user.user_login_claiming,
      aggregate.user.user_login_linking,
      aggregate.user.personal_data_erasure_run,
    ]

    to = [
      event.user.user_personal_data_erasure_applying,
      event.user.user_login_released,
      event.user.user_login_unlinked,
      event.user.user_personal_data_erasure_applied,
      event.user.personal_data_erasure_run_user_erased,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "run_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-0000000000aa"
    }
  }

  scenario "scenario_01_pending_erasure_is_applied" {
    title = "Apply a pending erasure"

    given {
      title = "An erasure was requested"
      event = event.user.user_personal_data_erasure_requested
    }

    given {
      title = "The user login is linked to the user"
      event = event.user.user_login_linked
    }

    given {
      title = "The run contains the user"
      event = event.user.personal_data_erasure_run_started
    }

    when {
      title   = "Apply the erasure"
      command = command.apply_user_personal_data_erasure
    }

    then {
      title = "The erasure starts applying"
      event = event.user.user_personal_data_erasure_applying
    }

    then {
      title = "The user login is released"
      event = event.user.user_login_released
    }

    then {
      title = "The user login is unlinked from the user"
      event = event.user.user_login_unlinked
    }

    then {
      title = "The erasure is applied"
      event = event.user.user_personal_data_erasure_applied
    }

    then {
      title = "The run records the user as erased"
      event = event.user.personal_data_erasure_run_user_erased
    }

    comment {
      description = "The releases are atomic and Applied is written only after the truncate, so a failed step leaves the user pending and the next run retries it. A later scavenge removes the truncated events from disk. The erasure stream is the source of truth for the status."
    }
  }

  scenario "scenario_02_canceled_erasure_is_not_applied" {
    title = "Apply an erasure that was canceled"

    given {
      title = "The erasure was canceled"
      event = event.user.user_personal_data_erasure_canceled
    }

    when {
      title   = "Apply the canceled erasure"
      command = command.apply_user_personal_data_erasure
    }

    then {
      title = "There is no pending erasure to apply"
      error = "erasure_not_requested"
    }
  }

  scenario "scenario_03_unknown_user_personal_data_is_not_applied" {
    title = "Apply an erasure for an unknown user"

    when {
      title   = "Apply the erasure for an unknown user_id"
      command = command.apply_user_personal_data_erasure
    }

    then {
      title = "No personal data exists for the given user"
      error = "user_personal_data_not_found"
    }
  }

  scenario "scenario_04_erasure_is_applied_concurrently" {
    title = "Another request changes the erasure first"

    given {
      title = "An erasure was requested"
      event = event.user.user_personal_data_erasure_requested
    }

    when {
      title   = "Apply the erasure while another request changes the erasure process"
      command = command.apply_user_personal_data_erasure
    }

    then {
      title = "This attempt conflicts because the erasure process changed before the apply was saved"
      error = "concurrent_update"
    }

    comment {
      description = "The next run retries the user. Releasing an already released login and truncating an already truncated stream are no-ops."
    }
  }
}
