# Chapter 4 - Business Onboarding workflows, in board order.

state_change "initiate_business_onboarding" {
  title       = "Initiate Business Onboarding"
  description = "POST /business-onboarding/initiate starts a business onboarding process for an authenticated, fully onboarded user. The mapper generates the business onboarding id, trims the legal name, and takes InitiatedBy from the app-owned user_id claim. The handler appends BusinessOnboardingInitiated to a single process stream. Repeating the request creates another business onboarding."
  status      = "done"

  screen "initiate_business_onboarding_ui" {
    title = "Initiate Business Onboarding"
    actor = actor.authenticated_user
    to    = [command.initiate_business_onboarding]

    field "legal_name" {
      type    = "String"
      example = "Acme Ltd"
    }
  }

  command "initiate_business_onboarding" {
    title       = "Initiate Business Onboarding"
    description = "Starts a business onboarding process for the authenticated user. Requires the user-required policy and the app-owned user_id claim."
    aggregate   = aggregate.business_onboarding.business_onboarding
    to = [
      event.business_onboarding.business_onboarding_initiated
    ]

    field "business_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000100"
    }

    field "legal_name" {
      type    = "String"
      example = "Acme Ltd"
    }

    field "initiated_by" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }
  }

  scenario "scenario_01_business_onboarding_is_initiated" {
    title = "An onboarded user starts business onboarding"

    when {
      title   = "Initiate business onboarding with a legal name"
      command = command.initiate_business_onboarding
    }

    then {
      title = "The business onboarding is initiated"
      event = event.business_onboarding.business_onboarding_initiated
    }

    comment {
      description = "The mapper generates the business onboarding id, trims the legal name, and takes InitiatedBy from the app-owned user_id claim. The handler appends one event to the new process stream."
    }
  }

  scenario "scenario_02_business_onboarding_id_already_has_events" {
    title = "The generated business onboarding ID already has events"

    when {
      title   = "Initiate business onboarding with a generated ID that already has events"
      command = command.initiate_business_onboarding
    }

    then {
      title = "The generated business onboarding ID is already associated with an existing process"
      error = "onboarding_state_unexpected"
    }
  }

  scenario "scenario_03_business_onboarding_is_initiated_concurrently" {
    title = "Another request changes the business onboarding stream first"

    when {
      title   = "Initiate business onboarding while another request changes the stream"
      command = command.initiate_business_onboarding
    }

    then {
      title = "This attempt conflicts because the stream changed before initiation was saved"
      error = "concurrent_update"
    }

    comment {
      description = "The handler appends with the expected stream version. If another request writes first, the command returns a conflict."
    }
  }
}

state_view "retrieve_business_onboarding" {
  title       = "Retrieve Business Onboarding"
  description = "GET /business-onboarding/{id} returns the persisted Business Onboarding projection. The read model's id and the screen's business_onboarding_id identify the same business onboarding."
  status      = "done"

  readmodel "business_onboarding" {
    title       = "Business Onboarding"
    description = "Persistent Business Onboarding read model assembled from business onboarding events. It is exposed by GET /business-onboarding/{id} and returns one business onboarding; it is not paginated."
    question    = "What is the current state of this business onboarding?"
    from = [
      event.business_onboarding.business_onboarding_initiated
    ]
    to = [screen.retrieve_business_onboarding_ui]

    field "id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000100"
    }

    field "legal_name" {
      type    = "String"
      example = "Acme Ltd"
    }

    field "initiated_by" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }

    field "status" {
      type    = "String"
      example = "in_progress"
    }

    field "completed_at" {
      type     = "DateTime"
      optional = true
      example  = null
    }

    field "canceled_at" {
      type     = "DateTime"
      optional = true
      example  = null
    }

    field "cancellation_reason" {
      type     = "String"
      optional = true
      example  = null
    }

    field "created_at" {
      type    = "DateTime"
      example = "2026-01-01T00:00:00.123Z"
    }

    field "last_modified_at" {
      type    = "DateTime"
      example = "2026-01-01T00:00:00.123Z"
    }

    field "revision" {
      type    = "Long"
      example = 1
    }
  }

  screen "retrieve_business_onboarding_ui" {
    title = "Business Onboarding"
    actor = actor.authenticated_user

    field "business_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000100"
    }

    field "legal_name" {
      type    = "String"
      example = "Acme Ltd"
    }

    field "status" {
      type    = "String"
      example = "in_progress"
    }

  }

  scenario "scenario_01_business_onboarding_is_retrieved" {
    title = "Business onboarding view"

    given {
      title = "A business onboarding is initiated"
      event = event.business_onboarding.business_onboarding_initiated
    }

    then {
      title     = "The business onboarding details are projected into the business onboarding document"
      readmodel = readmodel.business_onboarding
    }

    comment {
      description = "The read model exposes the business onboarding details and current status."
    }
  }

  scenario "scenario_02_business_onboarding_is_not_found" {
    title = "No business onboarding read model is available for the requested ID"

    given {
      title = "The business onboarding event has not been projected into the Business Onboarding read model"
      event = event.business_onboarding.business_onboarding_initiated
    }

    then {
      title = "The business onboarding cannot be retrieved"
      error = "business_onboarding_not_found"
    }

    comment {
      description = "The query returns business_onboarding_not_found when the Business Onboarding document is absent, including before the initiation event is projected."
    }
  }
}

state_change "record_business_onboarding_address" {
  title       = "Record Business Onboarding Address"
  description = "POST /business-onboarding/{id}/record-address records an address for the business onboarding. A business onboarding can have only one address; recording a different address after one is already recorded is rejected. Recording the exact same address again returns OK without appending a new event."
  status      = "done"

  screen "record_business_onboarding_address_ui" {
    title = "Record an Address"
    actor = actor.authenticated_user
    to    = [command.record_business_onboarding_address]

    field "business_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000100"
    }

    field "line1" {
      type    = "String"
      example = "Avenida San Javier 67"
    }

    field "line2" {
      type    = "String"
      example = "Portal 15, 4ºF"
    }

    field "town_name" {
      type    = "String"
      example = "Málaga"
    }

    field "post_code" {
      type    = "String"
      example = "29140"
    }

    field "country_subdivision" {
      type    = "String"
      example = "Málaga"
    }

    field "country" {
      type    = "String"
      example = "ES"
    }
  }

  command "record_business_onboarding_address" {
    title       = "Record Business Onboarding Address"
    description = "Records the address of the given business onboarding. The address is recorded only once; repeating the exact same address is idempotent."
    aggregate   = aggregate.business_onboarding.business_onboarding
    to = [
      event.business_onboarding.business_onboarding_address_recorded
    ]

    field "business_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000100"
    }

    field "line1" {
      type    = "String"
      example = "Avenida San Javier 67"
    }

    field "line2" {
      type    = "String"
      example = "Portal 15, 4ºF"
    }

    field "town_name" {
      type    = "String"
      example = "Málaga"
    }

    field "post_code" {
      type    = "String"
      example = "29140"
    }

    field "country_subdivision" {
      type    = "String"
      example = "Málaga"
    }

    field "country" {
      type    = "String"
      example = "ES"
    }

    field "recorded_by" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }
  }

  scenario "scenario_01_address_is_recorded" {
    title = "Record an address for a business onboarding"

    given {
      title = "The business onboarding exists"
      event = event.business_onboarding.business_onboarding_initiated
    }

    when {
      title   = "Record an address"
      command = command.record_business_onboarding_address
    }

    then {
      title = "The address is recorded for the business onboarding"
      event = event.business_onboarding.business_onboarding_address_recorded
    }

    comment {
      description = "The business onboarding has no address yet. Recording the exact same address again returns OK without appending another event."
    }
  }

  scenario "scenario_02_address_is_already_recorded" {
    title = "A different address is recorded after the business onboarding already has one"

    given {
      title = "The business onboarding exists"
      event = event.business_onboarding.business_onboarding_initiated
    }

    given {
      title = "An address is already recorded"
      event = event.business_onboarding.business_onboarding_address_recorded
    }

    when {
      title   = "Record a different address for the business onboarding"
      command = command.record_business_onboarding_address
    }

    then {
      title = "The existing address is kept and the different address is rejected"
      error = "address_already_recorded"
    }

    comment {
      description = "Only one address can be recorded. The exact same address is idempotent and returns OK without appending another event; a different address is rejected without appending events."
    }
  }

  scenario "scenario_03_unknown_business_onboarding_cannot_record_address" {
    title = "The business onboarding does not exist"

    when {
      title   = "Record an address for an unknown business onboarding ID"
      command = command.record_business_onboarding_address
    }

    then {
      title = "No business onboarding process exists to record an address for"
      error = "onboarding_state_unexpected"
    }
  }

  scenario "scenario_04_address_is_recorded_concurrently" {
    title = "Another request changes the business onboarding first"

    given {
      title = "The business onboarding exists"
      event = event.business_onboarding.business_onboarding_initiated
    }

    when {
      title   = "Record an address while another request changes the business onboarding"
      command = command.record_business_onboarding_address
    }

    then {
      title = "This attempt conflicts because the business onboarding changed before the address was saved"
      error = "concurrent_update"
    }

    comment {
      description = "The handler appends with the expected stream version. If another request writes first, the command returns a conflict."
    }
  }
}
