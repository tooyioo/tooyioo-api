# Chapter 1 - User Onboarding workflows, in board order.

state_change "initiate_user_onboarding" {
  title       = "Initiate User Onboarding"
  description = "POST /user-onboarding/initiate starts an onboarding for an unclaimed external identity and claims it. It atomically writes the User Onboarding and External Identity Claiming streams. A retry with an already claimed identity returns the existing onboarding ID without appending events."
  status      = "done"

  screen "initiate_user_onboarding_ui" {
    title = "Choose an Identity Provider"
    actor = actor.authenticated_user
    to    = [command.initiate_user_onboarding]

    field "external_identity_provider" {
      type    = "String"
      example = "Google"
    }
  }

  command "initiate_user_onboarding" {
    title       = "Initiate User Onboarding"
    description = "Personal details and external identity details are extracted from the id_token issued by the selected external_identity_provider. The external identity is used to look up Claimed Identity. If already claimed, the existing user onboarding is returned without appending events; otherwise onboarding is initiated and the identity is claimed."
    aggregate   = aggregate.onboarding.user_onboarding
    aggregate_dependencies = [
      aggregate.onboarding.external_identity_claiming,
    ]
    to = [
      event.onboarding.user_onboarding_initiated,
      event.onboarding.external_identity_claimed,
    ]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "name" {
      type    = "String"
      example = "Joe"
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "external_identity_id" {
      type         = "String"
      id_attribute = true
      example      = "abc123"
    }

    field "external_identity_provider" {
      type    = "String"
      example = "Google"
    }

    field "external_identity_issuer" {
      type    = "String"
      example = "https://accounts.google.com"
    }
  }

  scenario "scenario_01_google_user_onboarding_is_initiated_with_unclaimed_external_identity" {
    title = "The user chooses Google to begin onboarding with an unclaimed external identity"

    when {
      title   = "Initiate onboarding with the external identity returned by Google"
      command = command.initiate_user_onboarding
    }

    then {
      title = "The user onboarding is initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    then {
      title = "The external identity is claimed"
      event = event.onboarding.external_identity_claimed
    }

    comment {
      description = "When the user initiates onboarding with a valid external identity that has not been claimed, the system creates the onboarding and claims the identity. It appends UserOnboardingInitiated and ExternalIdentityClaimed."
    }
  }

  scenario "scenario_02_external_identity_is_claimed_concurrently" {
    title = "Another request claims the identity first"

    when {
      title   = "Initiate another onboarding for the given external identity"
      command = command.initiate_user_onboarding
    }

    then {
      title = "This attempt conflicts because another request claimed the external identity first"
      error = "concurrent_update"
    }

    comment {
      description = "This attempt returns a concurrency conflict. Retrying is safe: once the committed identity claim is visible, the same external identity returns its existing onboarding ID without appending events."
    }
  }

  scenario "scenario_03_new_user_onboarding_id_is_not_empty" {
    title = "The generated onboarding ID has unexpected state"

    when {
      title   = "Initiate onboarding with a generated ID that already has events"
      command = command.initiate_user_onboarding
    }

    then {
      title = "The generated onboarding ID is already associated with an existing process"
      error = "onboarding_state_unexpected"
    }
  }
}

state_view "retrieve_claimed_identity" {
  title       = "Retrieve Claimed Identity"
  description = "This read model is an in-memory lookup, not an HTTP resource. Initiate User Onboarding uses it to determine whether the external identity is already claimed globally and, if so, which onboarding owns it."
  status      = "done"

  readmodel "claimed_identity" {
    title       = "Claimed Identity"
    description = "In-memory command-side state rehydrated from the External Identity Claiming stream. It is not an HTTP resource and is not paginated."
    question    = "What is the user onboarding that claimed this external identity?"
    from = [
      event.onboarding.external_identity_claimed
    ]

    field "external_identity_id" {
      type         = "String"
      id_attribute = true
      example      = "abc123"
    }

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }

  scenario "scenario_01_external_identity_is_claimed" {
    title = "External identity claimed view"

    given {
      title = "The external identity is claimed"
      event = event.onboarding.external_identity_claimed
    }

    then {
      title     = "The user onboarding Id is projected into the external identity view"
      readmodel = readmodel.claimed_identity
    }

    comment {
      description = "The view exposes the association between an external identity and a user onboarding Id"
    }
  }
}

state_view "retrieve_user_onboarding" {
  title       = "Retrieve User Onboarding"
  description = "Retrieves the persisted User Onboarding projection through GET /user-onboarding/{id}. The read model's id and the screen's user_onboarding_id identify the same onboarding."
  status      = "done"

  readmodel "user_onboarding" {
    title       = "User Onboarding"
    description = "Persistent User Onboarding read model assembled from onboarding events. It is exposed by GET /user-onboarding/{id} and returns one onboarding; it is not paginated."
    question    = "What is the current state of this user onboarding?"
    from = [
      event.onboarding.user_onboarding_initiated
    ]
    to = [screen.retrieve_user_onboarding_ui]

    field "id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "name" {
      type    = "String"
      example = "Joe"
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = null
    }

    field "external_identity_id" {
      type    = "String"
      example = "abc123"
    }

    field "external_identity_provider" {
      type    = "String"
      example = "Google"
    }

    field "external_identity_issuer" {
      type    = "String"
      example = "https://accounts.google.com"
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

  screen "retrieve_user_onboarding_ui" {
    title = "User Onboarding"
    actor = actor.authenticated_user

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "name" {
      type    = "String"
      example = "Joe"
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = null
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
  }

  scenario "scenario_01_user_onboarding_is_retrieved" {
    title = "User onboarding view"

    given {
      title = "A user onboarding is initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    then {
      title     = "The user onboarding details are projected into the user onboarding document"
      readmodel = readmodel.user_onboarding
    }

    comment {
      description = "The read model exposes the user onboarding details and current status."
    }
  }

  scenario "scenario_02_user_onboarding_is_not_found" {
    title = "No user onboarding read model is available for the requested ID"

    given {
      title = "The onboarding event has not been projected into the User Onboarding read model"
      event = event.onboarding.user_onboarding_initiated
    }

    then {
      title = "The user onboarding cannot be retrieved"
      error = "user_onboarding_not_found"
    }

    comment {
      description = "The query returns user_onboarding_not_found when the User Onboarding document is absent, including before the initiation event is projected. The other User Onboarding state views share this endpoint and read model."
    }
  }
}

state_change "choose_user_onboarding_alias" {
  title       = "Choose User Onboarding Alias"
  description = "POST /user-onboarding/{id}/choose-alias claims an available alias and assigns it to the user onboarding. It atomically writes the User Onboarding and Alias Claiming streams. Choosing the same alias again succeeds without new events; an alias owned by another onboarding is rejected."
  status      = "done"

  screen "choose_user_onboarding_alias_ui" {
    title = "Choose an Alias"
    actor = actor.authenticated_user
    to    = [command.choose_user_onboarding_alias]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "alias" {
      type    = "String"
      example = "joe-1984"
    }
  }

  command "choose_user_onboarding_alias" {
    title       = "Choose User Onboarding Alias"
    description = "Chooses a globally unique alias for the given user onboarding."
    aggregate   = aggregate.onboarding.user_onboarding
    aggregate_dependencies = [
      aggregate.onboarding.alias_claiming,
    ]
    to = [
      event.onboarding.user_onboarding_alias_chosen,
      event.onboarding.alias_claimed,
    ]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "alias" {
      type         = "String"
      id_attribute = true
      example      = "joe-1984"
    }
  }

  scenario "scenario_01_available_alias_is_claimed" {
    title = "Choose an available alias"

    given {
      title = "The user onboarding exists"
      event = event.onboarding.user_onboarding_initiated
    }

    when {
      title   = "Choose an alias that is not claimed"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "The alias is chosen for the user onboarding"
      event = event.onboarding.user_onboarding_alias_chosen
    }

    then {
      title = "The alias is claimed globally"
      event = event.onboarding.alias_claimed
    }

    comment {
      description = "The alias is available and the onboarding has no alias. Both events are appended in one multi-stream operation."
    }
  }

  scenario "scenario_02_alias_is_already_claimed_by_another_onboarding" {
    title = "Alias is already in use"

    given {
      title = "Another user onboarding has claimed the alias"
      event = event.onboarding.alias_claimed
    }

    when {
      title   = "Choose the same alias for this onboarding"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "The alias remains claimed by another onboarding and cannot be chosen"
      error = "alias_already_claimed"
    }

    comment {
      description = "The existing alias claim points to a different user onboarding. The command returns an already-in-use error without appending events."
    }
  }

  scenario "scenario_03_different_alias_was_already_chosen" {
    title = "The onboarding already has another alias"

    given {
      title = "A different alias was chosen for this onboarding"
      event = event.onboarding.user_onboarding_alias_chosen
    }

    when {
      title   = "Choose a new alias for the same onboarding"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "The onboarding keeps its existing alias; the requested replacement is rejected"
      error = "alias_already_chosen"
    }

    comment {
      description = "The newly requested alias is available, but this onboarding already has a different alias. The handler rejects replacement without appending events."
    }
  }

  scenario "scenario_04_unknown_user_onboarding_cannot_choose_alias" {
    title = "The user onboarding does not exist"

    when {
      title   = "Choose an alias for an unknown onboarding ID"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "No onboarding process exists to choose an alias for"
      error = "onboarding_state_unexpected"
    }
  }

  scenario "scenario_05_canceled_user_onboarding_cannot_choose_alias" {
    title = "A canceled onboarding cannot choose an alias"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    given {
      title = "The user onboarding was canceled"
      event = event.onboarding.user_onboarding_canceled
    }

    when {
      title   = "Attempt to choose an alias for the canceled onboarding"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "An onboarding that is no longer in progress cannot be assigned an alias"
      error = "onboarding_not_in_progress"
    }
  }

  scenario "scenario_06_completed_user_onboarding_cannot_choose_alias" {
    title = "A completed onboarding cannot choose an alias"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    given {
      title = "The user onboarding was completed"
      event = event.onboarding.user_onboarding_completed
    }

    when {
      title   = "Choose an alias after the onboarding was completed"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "An onboarding that is no longer in progress cannot be assigned an alias"
      error = "onboarding_not_in_progress"
    }
  }

  scenario "scenario_07_alias_is_claimed_concurrently" {
    title = "Another request claims the alias first"

    given {
      title = "The user onboarding exists"
      event = event.onboarding.user_onboarding_initiated
    }

    when {
      title   = "Choose an alias while another request claims it"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "This attempt conflicts because the alias claim changed before it was saved"
      error = "concurrent_update"
    }

    comment {
      description = "This attempt returns a concurrency conflict. Retrying is safe: if the same onboarding claimed the alias, the retry succeeds without another event; if another onboarding claimed it, the retry reports that the alias is in use."
    }
  }
}

state_view "retrieve_claimed_alias" {
  title       = "Retrieve Claimed Alias"
  description = "This read model is an in-memory lookup, not an HTTP resource. Choose User Onboarding Alias uses it to determine whether an alias is already claimed globally and, if so, which onboarding owns it."
  status      = "done"

  readmodel "claimed_alias" {
    title       = "Claimed Alias"
    description = "In-memory command-side state rehydrated from the Alias Claiming stream. It is not an HTTP resource and is not paginated."
    question    = "Which user onboarding has claimed this alias?"
    from        = [event.onboarding.alias_claimed]

    field "alias" {
      type         = "String"
      id_attribute = true
      example      = "joe-1984"
    }

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }

  scenario "scenario_01_alias_claim_is_projected" {
    title = "Claimed alias view"

    given {
      title = "The alias is claimed"
      event = event.onboarding.alias_claimed
    }

    then {
      title     = "The alias resolves to its user onboarding"
      readmodel = readmodel.claimed_alias
    }

    comment {
      description = "The alias is the Alias Claiming stream ID; Alias Claimed supplies the associated user onboarding ID."
    }
  }
}

state_view "retrieve_user_onboarding_after_alias_chosen" {
  title       = "Retrieve User Onboarding After Alias Chosen"
  description = "Retrieves the persisted User Onboarding projection through GET /user-onboarding/{id}. The read model's id and the screen's user_onboarding_id identify the same onboarding."
  status      = "done"

  readmodel "user_onboarding" {
    title       = "User Onboarding"
    description = "Persistent User Onboarding read model assembled from onboarding events. It is exposed by GET /user-onboarding/{id} and returns one onboarding; it is not paginated."
    question    = "What are the current onboarding details after an alias is chosen?"
    from = [
      event.onboarding.user_onboarding_initiated,
      event.onboarding.user_onboarding_alias_chosen,
    ]
    to = [screen.retrieve_user_onboarding_ui]

    field "id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "name" {
      type    = "String"
      example = "Joe"
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = "joe-1984"
    }

    field "external_identity_id" {
      type    = "String"
      example = "abc123"
    }

    field "external_identity_provider" {
      type    = "String"
      example = "Google"
    }

    field "external_identity_issuer" {
      type    = "String"
      example = "https://accounts.google.com"
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
      example = "2026-01-01T00:05:00.123Z"
    }

    field "revision" {
      type    = "Long"
      example = 2
    }
  }

  screen "retrieve_user_onboarding_ui" {
    title = "User Onboarding"
    actor = actor.authenticated_user

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "name" {
      type    = "String"
      example = "Joe"
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = "joe-1984"
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
      example = "2026-01-01T00:05:00.123Z"
    }
  }

  scenario "scenario_01_user_onboarding_shows_chosen_alias" {
    title = "User onboarding view after alias selection"

    given {
      title = "The user onboarding is initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    given {
      title = "An alias is chosen"
      event = event.onboarding.user_onboarding_alias_chosen
    }

    then {
      title     = "The user onboarding shows the chosen alias"
      readmodel = readmodel.user_onboarding
    }

    comment {
      description = "The same User Onboarding projection now includes the chosen alias, a later modification time, and a higher revision."
    }
  }
}

state_change "complete_user_onboarding" {
  title       = "Complete User Onboarding"
  description = "POST /user-onboarding/{id}/complete completes an existing onboarding that already has a chosen alias, creates a user ID, and records the accepted terms version. Repeating completion returns the existing user ID without appending another event."
  status      = "done"

  screen "complete_user_onboarding_ui" {
    title = "Complete Onboarding"
    actor = actor.authenticated_user
    to    = [command.complete_user_onboarding]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "terms_and_conditions_version" {
      type    = "String"
      example = "1.0.0"
    }
  }

  command "complete_user_onboarding" {
    title       = "Complete User Onboarding"
    description = "Completes the given user onboarding."
    aggregate   = aggregate.onboarding.user_onboarding
    to = [
      event.onboarding.user_onboarding_completed
    ]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "terms_and_conditions_version" {
      type    = "String"
      example = "1.0.0"
    }
  }

  scenario "scenario_01_user_onboarding_is_completed" {
    title = "Complete an existing user onboarding"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    when {
      title   = "Accept terms and complete onboarding"
      command = command.complete_user_onboarding
    }

    then {
      title = "The onboarding is completed and a user ID is assigned"
      event = event.onboarding.user_onboarding_completed
    }

    comment {
      description = "The handler generates the user ID and appends User Onboarding Completed with the terms and conditions version. Completion requires that an alias was already chosen."
    }
  }

  scenario "scenario_02_user_onboarding_without_alias_cannot_be_completed" {
    title = "An onboarding without an alias cannot be completed"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    when {
      title   = "Attempt to complete onboarding without an alias"
      command = command.complete_user_onboarding
    }

    then {
      title = "The onboarding is not completed because no alias was chosen"
      error = "onboarding_alias_not_chosen"
    }

    comment {
      description = "Choosing an alias is a prerequisite for completion. The handler returns a conflict and appends no events."
    }
  }

  scenario "scenario_03_unknown_user_onboarding_cannot_be_completed" {
    title = "The user onboarding does not exist"

    when {
      title   = "Complete an unknown onboarding ID"
      command = command.complete_user_onboarding
    }

    then {
      title = "No onboarding process exists to complete"
      error = "onboarding_state_unexpected"
    }
  }

  scenario "scenario_04_canceled_user_onboarding_cannot_be_completed" {
    title = "A canceled onboarding cannot be completed"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    given {
      title = "The user onboarding was canceled"
      event = event.onboarding.user_onboarding_canceled
    }

    when {
      title   = "Attempt to complete the canceled onboarding"
      command = command.complete_user_onboarding
    }

    then {
      title = "A canceled onboarding cannot be completed"
      error = "onboarding_already_canceled"
    }
  }

  scenario "scenario_05_user_onboarding_is_completed_concurrently" {
    title = "Another request changes the onboarding first"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    when {
      title   = "Complete onboarding while another request changes it"
      command = command.complete_user_onboarding
    }

    then {
      title = "This attempt conflicts because the onboarding changed before completion was saved"
      error = "concurrent_update"
    }

    comment {
      description = "This attempt returns a concurrency conflict. Retrying is safe: if another request completed the onboarding, the retry returns its existing user ID without appending another event; if cancellation won, the retry reports that the onboarding is canceled."
    }
  }
}

state_view "retrieve_user_onboarding_after_completion" {
  title       = "Retrieve User Onboarding After Completion"
  description = "Retrieves the persisted User Onboarding projection through GET /user-onboarding/{id}. The read model's id and the screen's user_onboarding_id identify the same onboarding."
  status      = "done"

  readmodel "user_onboarding" {
    title       = "User Onboarding"
    description = "Persistent User Onboarding read model assembled from onboarding events. It is exposed by GET /user-onboarding/{id} and returns one onboarding; it is not paginated."
    question    = "What are the completed onboarding details?"
    from = [
      event.onboarding.user_onboarding_initiated,
      event.onboarding.user_onboarding_alias_chosen,
      event.onboarding.user_onboarding_completed,
    ]
    to = [screen.retrieve_user_onboarding_ui]

    field "id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "name" {
      type    = "String"
      example = "Joe"
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = "joe-1984"
    }

    field "external_identity_id" {
      type    = "String"
      example = "abc123"
    }

    field "external_identity_provider" {
      type    = "String"
      example = "Google"
    }

    field "external_identity_issuer" {
      type    = "String"
      example = "https://accounts.google.com"
    }

    field "status" {
      type    = "String"
      example = "completed"
    }

    field "completed_at" {
      type     = "DateTime"
      optional = true
      example  = "2026-01-01T00:10:00.123Z"
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
      example = "2026-01-01T00:10:00.123Z"
    }

    field "revision" {
      type    = "Long"
      example = 3
    }
  }

  screen "retrieve_user_onboarding_ui" {
    title = "User Onboarding"
    actor = actor.authenticated_user

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "name" {
      type    = "String"
      example = "Joe"
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = "joe-1984"
    }

    field "status" {
      type    = "String"
      example = "completed"
    }

    field "completed_at" {
      type     = "DateTime"
      optional = true
      example  = "2026-01-01T00:10:00.123Z"
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
      example = "2026-01-01T00:10:00.123Z"
    }
  }

  scenario "scenario_01_completed_user_onboarding_is_retrieved" {
    title = "Completed user onboarding view"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    given {
      title = "An alias was chosen"
      event = event.onboarding.user_onboarding_alias_chosen
    }

    given {
      title = "The user onboarding was completed"
      event = event.onboarding.user_onboarding_completed
    }

    then {
      title     = "The view shows completed status and completion time"
      readmodel = readmodel.user_onboarding
    }

    comment {
      description = "This is the same User Onboarding document after its completion event. The chosen alias is present in this example, although the completion handler does not require it."
    }
  }
}

state_view "project_users" {
  title       = "Project Users"
  description = "Projects UserOnboardingCompleted into the shared Users read model consumed by the User Directory queries in Chapter 3. The queries add pagination and filters; this slice only defines the projection and its source event."
  status      = "done"

  readmodel "users" {
    title       = "Users"
    description = "Shared persisted Users read model populated by UserOnboardingCompleted. It is the projection behind GET /users and GET /users/summaries; it is not itself an HTTP resource and does not apply pagination or filters. Those query concerns are defined by Retrieve Users and Retrieve User Summaries in Chapter 3."
    question    = "Which users have been projected from UserOnboardingCompleted?"
    from        = [event.onboarding.user_onboarding_completed]

    field "items" {
      cardinality = "List"
      type        = field_type.onboarding.user
      example = [{
        id                           = "00000000-0000-0000-0000-000000000010"
        alias                        = "joe-bloggs-spain_123"
        name                         = "Joe"
        last_name                    = "Bloggs"
        email                        = "joe@test.com"
        is_email_verified            = true
        phone_number                 = "+34644000000"
        external_id                  = "google-sub-123"
        external_provider            = "Google"
        user_onboarding_id           = "00000000-0000-0000-0000-000000000001"
        created_at                   = "2026-01-01T12:05:00.000Z"
        last_modified_at             = "2026-01-01T12:05:00.000Z"
        terms_and_conditions_version = "v1"
      }]
    }
  }

  scenario "scenario_01_users_are_projected_after_completion" {
    title = "Users are projected after an onboarding completes"

    given {
      title = "The user onboarding was completed"
      event = event.onboarding.user_onboarding_completed
    }

    then {
      title     = "The Users read model contains the completed user"
      readmodel = readmodel.users
    }

    comment {
      description = "The projection is shared by the User Directory queries in Chapter 3. Those queries declare their own Users read model shape for pagination and filtering."
    }
  }
}

state_view "retrieve_user_profile" {
  title       = "Retrieve User Profile"
  description = "After onboarding completes, the User projection creates a profile from the completed onboarding. The User read model's id is the user_id from UserOnboardingCompleted and is exposed as id by GET /users/{id}; the profile screen labels it user_id."
  status      = "done"

  readmodel "user" {
    title       = "User"
    description = "Shared persisted User read model populated by UserOnboardingCompleted. It is exposed by GET /users/{id} and returns one profile; it is not paginated."
    question    = "What profile belongs to this user ID?"
    from        = [event.onboarding.user_onboarding_completed]
    to          = [screen.user_profile]

    field "id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "alias" {
      type    = "String"
      example = "joe-bloggs-spain_123"
    }

    field "name" {
      type    = "String"
      example = "Joe"
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "phone_number" {
      type    = "String"
      example = ""
    }

    field "external_identity_id" {
      type    = "String"
      example = "google-sub-123"
    }

    field "external_identity_provider" {
      type    = "String"
      example = "Google"
    }

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }

    field "created_at" {
      type    = "DateTime"
      example = "2026-01-01T12:05:00.000Z"
    }

    field "last_modified_at" {
      type    = "DateTime"
      example = "2026-01-01T12:05:00.000Z"
    }

    field "terms_and_conditions_version" {
      type    = "String"
      example = "v1"
    }
  }

  screen "user_profile" {
    title = "User Profile"
    actor = actor.authenticated_user

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "alias" {
      type    = "String"
      example = "joe-bloggs-spain_123"
    }

    field "name" {
      type    = "String"
      example = "Joe"
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "phone_number" {
      type    = "String"
      example = ""
    }
  }

  scenario "scenario_01_completed_user_profile_is_retrieved" {
    title = "The user sees their profile after onboarding completes"

    given {
      title = "The user onboarding was completed and assigned a user ID"
      event = event.onboarding.user_onboarding_completed
    }

    then {
      title     = "The User read model contains the completed user's profile"
      readmodel = readmodel.user
    }

    comment {
      description = "The completion response supplies the user ID used to retrieve this profile. The User projector consumes UserOnboardingCompleted and retrieves the remaining profile details from the onboarding process."
    }
  }

  scenario "scenario_02_user_profile_is_not_available" {
    title = "No user profile is available for the requested ID"

    given {
      title = "The User read model does not contain the requested user"
      event = event.onboarding.user_onboarding_completed
    }

    then {
      title = "The user profile cannot be retrieved"
      error = "profile_not_found"
    }

    comment {
      description = "The profile query returns profile_not_found when the User document is absent, including while the completion event has not yet been projected."
    }
  }
}
