# Chapter 5 - User Personal Data and Erasure workflows, in board order.

state_view "retrieve_user_personal_data" {
  title       = "Retrieve User Personal Data"
  description = "GET /users/{user_id}/personal-data returns the authenticated user's personal data and erasure status."
  status      = "done"

  readmodel "user_personal_data" {
    title       = "User Personal Data"
    description = "Persisted User Personal Data read model. The only read model whose purpose is to expose PII to its owner. Anonymized on UserPersonalDataErasureApplied."
    question    = "Which personal data and erasure status does this user have?"
    from = [
      event.user.user_personal_data_recorded,
      event.user.user_personal_data_erasure_requested,
      event.user.user_personal_data_erasure_canceled,
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

    field "erase_after_utc" {
      type     = "DateTime"
      optional = true
      example  = "2026-11-16T00:00:00.000Z"
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

    field "erase_after_utc" {
      type     = "DateTime"
      optional = true
      example  = null
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

  scenario "scenario_02_pending_erasure_is_retrieved" {
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
      title     = "The view shows the pending erasure and its due date"
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
  description = "POST /users/{user_id}/personal-data/erase starts the right-to-be-forgotten grace period for the authenticated user. The data remains readable and the request can be canceled until the grace period ends."
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
    description = "Appends UserPersonalDataErasureRequested with the due date computed from the configured grace period."
    aggregate   = aggregate.user.user_personal_data
    api_endpoint = "POST /users/{user_id}/personal-data/erase"

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
      title = "The erasure is requested with a due date"
      event = event.user.user_personal_data_erasure_requested
    }

    comment {
      description = "A repeated request returns the existing due date without appending another event."
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
    title = "Another request changes the personal data first"

    given {
      title = "The personal data was recorded"
      event = event.user.user_personal_data_recorded
    }

    when {
      title   = "Request the erasure while another request changes the personal data"
      command = command.request_user_personal_data_erasure
    }

    then {
      title = "This attempt conflicts because the personal data changed before the request was saved"
      error = "concurrent_update"
    }
  }
}

state_change "cancel_user_personal_data_erasure" {
  title       = "Cancel User Personal Data Erasure"
  description = "POST /users/{user_id}/personal-data/cancel-erasure cancels a pending erasure during the grace period. It is rejected once the erasure has been applied."
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
    description = "Appends UserPersonalDataErasureCanceled. The personal data was never truncated."
    aggregate   = aggregate.user.user_personal_data
    api_endpoint = "POST /users/{user_id}/personal-data/cancel-erasure"

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
    title = "Cancel a pending erasure during the grace period"

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

  scenario "scenario_02_erased_user_personal_data_cannot_be_restored" {
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

  scenario "scenario_03_unknown_user_personal_data_cannot_cancel_an_erasure" {
    title = "Cancel the erasure of unknown personal data"

    when {
      title   = "Cancel the erasure for an unknown user_id"
      command = command.cancel_user_personal_data_erasure
    }

    then {
      title = "No personal data exists for the given user"
      error = "user_personal_data_not_found"
    }
  }
}

automation "apply_due_user_personal_data_erasures" {
  title       = "Apply Due User Personal Data Erasures"
  description = "Scheduled job; not an HTTP endpoint. Every few seconds it queries the due erasures read model for requests whose erase_after_utc has passed and applies them idempotently."
  status      = "done"

  readmodel "due_user_personal_data_erasures" {
    title       = "Due User Personal Data Erasures"
    description = "View read by the scheduled erasure processor. It contains erasure requests whose grace period has ended."
    question    = "Which personal data erasure requests are due?"

    from = [
      event.user.user_personal_data_erasure_requested,
      event.user.user_personal_data_erasure_canceled,
      event.user.user_personal_data_erasure_applied,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "status" {
      type    = "String"
      example = "erasure_requested"
    }

    field "erase_after_utc" {
      type    = "DateTime"
      example = "2026-11-16T00:00:00.000Z"
    }
  }

  command "apply_user_personal_data_erasure" {
    title       = "Apply User Personal Data Erasure"
    description = "Appends UserPersonalDataErasureApplied and releases every user login of the user, then truncates the user personal data stream before the erasure request. The erasure events survive and the PII disappears."
    aggregate   = aggregate.user.user_personal_data
    external_trigger = true

    aggregate_dependencies = [
      aggregate.user.user_login_claiming,
      aggregate.user.user_login_linking,
    ]

    to = [
      event.user.user_personal_data_erasure_applied,
      event.user.user_login_released,
      event.user.user_login_unlinked,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }
  }



  scenario "scenario_01_due_erasure_is_applied" {
    title = "Apply an erasure whose grace period ended"

    given {
      title = "The user login is linked to the user"
      event = event.user.user_login_linked
    }

    given {
      title     = "The due erasures view contains the request"
      readmodel = readmodel.due_user_personal_data_erasures
    }

    when {
      title   = "Apply the due erasure"
      command = command.apply_user_personal_data_erasure
    }

    then {
      title = "The erasure is applied"
      event = event.user.user_personal_data_erasure_applied
    }

    then {
      title = "The user login is released"
      event = event.user.user_login_released
    }

    then {
      title = "The user login is unlinked"
      event = event.user.user_login_unlinked
    }

    comment {
      description = "After the atomic append, the user personal data stream is truncated before UserPersonalDataErasureRequested, so the request and applied events remain and the PII disappears. A later scavenge removes the truncated events from disk."
    }
  }

  scenario "scenario_02_erasure_before_its_due_date_is_not_applied" {
    title = "Apply an erasure whose grace period has not ended"

    given {
      title = "An erasure was requested"
      event = event.user.user_personal_data_erasure_requested
    }

    when {
      title   = "Apply the erasure before its due date"
      command = command.apply_user_personal_data_erasure
    }

    then {
      title = "The erasure is not due"
      error = "erasure_not_due"
    }
  }

  scenario "scenario_03_erasure_without_a_request_is_not_applied" {
    title = "Apply an erasure that was never requested"

    when {
      title   = "Apply an erasure with no request"
      command = command.apply_user_personal_data_erasure
    }

    then {
      title = "No erasure request exists"
      error = "erasure_not_requested"
    }
  }
}
