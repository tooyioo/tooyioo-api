# Actors

actor "anonymous_user" {
  title         = "User"
  auth_required = false
}

actor "authenticated_user" {
  title         = "User"
  auth_required = true
}

bounded_context "onboarding" {
  title = "User Onboarding"

  aggregate "user_onboarding" {
    title       = "User Onboarding"
    description = "Stream of events associated to a user onboarding process."
  }

  aggregate "external_identity_claiming" {
    title       = "External Identity Claiming"
    description = "Stream of events associated to an external identity claiming process."
  }

  aggregate "alias_claiming" {
    title       = "Alias Claiming"
    description = "Stream of events associated to an alias claiming process."
  }

  event "user_onboarding_initiated" {
    title       = "User Onboarding Initiated"
    description = "A user onboarding process was initiated with a given external identity Id."
    aggregate   = aggregate.user_onboarding

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
  }

  event "user_onboarding_alias_chosen" {
    title       = "User Onboarding Alias Chosen"
    description = "An alias was chosen for a given user onboarding."
    aggregate   = aggregate.user_onboarding

    field "alias" {
      type    = "String"
      example = "joe-1984"
    }
  }

  event "user_onboarding_completed" {
    title       = "User Onboarding Completed"
    description = "A given user onboarding is completed."
    aggregate   = aggregate.user_onboarding

    field "terms_and_conditions_version" {
      type    = "String"
      example = "1.0.0"
    }

    field "user_id" {
      type    = "String"
      example = "00000001-0000-0000-0000-000000000000"
    }
  }

  event "user_onboarding_canceled" {
    title       = "User Onboarding Canceled"
    description = "A user onboarding was canceled."
    aggregate   = aggregate.user_onboarding

    field "reason" {
      type    = "String"
      example = "Requested by user"
    }
  }

  event "external_identity_claimed" {
    title       = "External Identity Claimed"
    description = "An external identity was claimed."
    aggregate   = aggregate.external_identity_claiming

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }

  event "alias_claimed" {
    title       = "Alias Claimed"
    description = "An alias was claimed during user onboarding."
    aggregate   = aggregate.alias_claiming

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }
}

chapter "user_onboarding" {
  title = "Chapter 1 - User Onboarding"
  workflows = [
    workflow.initiate_user_onboarding,
    workflow.retrieve_claimed_identity,
    workflow.retrieve_user_onboarding,
    workflow.choose_user_onboarding_alias,
    workflow.retrieve_claimed_alias,
    workflow.retrieve_user_onboarding_after_alias_chosen,
    workflow.complete_user_onboarding,
    workflow.retrieve_user_onboarding_after_completion
  ]
}

state_change "initiate_user_onboarding" {
  title       = "Initiate User Onboarding"
  description = "An unclaimed external identity initiates a new onboarding and is claimed. A retry with an already claimed identity returns the existing onboarding ID without appending events."
  status      = "done"

  screen "initiate_user_onboarding_ui" {
    title = "Initiate User Onboarding UI"
    actor = actor.anonymous_user
    to    = [command.initiate_user_onboarding]

    field "provider" {
      type    = "String"
      example = "Google"
    }
  }

  screen_image "initiate_user_onboarding_mockup" {
    url = "./images/initiate-user-onboarding.png"
  }

  command "initiate_user_onboarding" {
    title       = "Initiate User Onboarding"
    description = "Uses the external identity from the provider's id_token to look up Claimed Identity. If it is already claimed, returns the existing user onboarding without appending events, in an idempotent manner. Otherwise, initiates onboarding and claims the identity."
    aggregate   = aggregate.onboarding.user_onboarding
    aggregate_dependencies = [
      aggregate.onboarding.external_identity_claiming,
    ]
    to = [
      event.onboarding.user_onboarding_initiated,
      event.onboarding.external_identity_claimed,
    ]

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
  }

  scenario "google_user_onboarding_is_initiated_with_unclaimed_external_identity" {
    title = "User Onboarding is initiated with an unclaimed Google's external identity"

    when {
      title   = "Initiate onboarding for a given external identity"
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

  scenario "external_identity_is_claimed_concurrently" {
    title = "Another request claims the identity first"

    when {
      title   = "Initiate another onboarding for the given external identity"
      command = command.initiate_user_onboarding
    }

    then {
      title = "HTTP 409 Conflict"
      error = "Could not initiate user onboarding because of a concurrency conflict, please retry"
    }

    comment {
      description = "Both requests read the identity as unclaimed. Another request commits its claim first, so this request receives an optimistic concurrency conflict."
    }
  }

  scenario "new_user_onboarding_id_is_not_empty" {
    title = "The generated onboarding ID has unexpected state"

    when {
      title   = "Initiate onboarding with a generated ID that already has events"
      command = command.initiate_user_onboarding
    }

    then {
      title = "HTTP 409 Conflict"
      error = "Could not initiate user onboarding because of an unexpected state conflict, please retry"
    }
  }
}

state_view "retrieve_claimed_identity" {
  title  = "Retrieve Claimed Identity"
  status = "done"

  readmodel "claimed_identity" {
    title       = "Claimed Identity"
    description = "Claimed Identity and associated user onboarding Id."
    question    = "What is the user onboarding that claimed this external identity?"
    from = [
      event.onboarding.external_identity_claimed
    ]

    field "id" {
      type         = "String"
      id_attribute = true
      example      = "abc123"
    }

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }

  scenario "external_identity_is_claimed" {
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
  title  = "Retrieve User Onboarding"
  status = "done"

  readmodel "user_onboarding" {
    title       = "User Onboarding"
    description = "Current user onboarding details assembled from the onboarding events."
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
    title = "Retrieve User Onboarding UI"
    actor = actor.authenticated_user
  }

  scenario "user_onboarding_is_retrieved" {
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
}

state_change "choose_user_onboarding_alias" {
  title       = "Choose User Onboarding Alias"
  description = "An available alias is claimed and assigned to the user onboarding. Choosing the same alias again succeeds without new events; an alias owned by another onboarding is rejected."
  status      = "done"

  screen "choose_user_onboarding_alias_ui" {
    title = "Choose User Onboarding Alias UI"
    actor = actor.authenticated_user
    to    = [command.choose_user_onboarding_alias]

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
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }

    field "alias" {
      type    = "String"
      example = "joe-1984"
    }
  }

  scenario "available_alias_is_claimed" {
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

  scenario "alias_is_already_claimed_by_another_onboarding" {
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
      title = "HTTP 400 Bad Request"
      error = "The provided user onboarding alias is already in use by onboarding with Id {userOnboardingId}"
    }

    comment {
      description = "The existing alias claim points to a different user onboarding. The command returns an already-in-use error without appending events."
    }
  }

  scenario "different_alias_was_already_chosen" {
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
      title = "HTTP 400 Bad Request"
      error = "A different alias {existingAlias} had already been chosen for this onboarding"
    }

    comment {
      description = "The newly requested alias is available, but this onboarding already has a different alias. The handler rejects replacement without appending events."
    }
  }

  scenario "unknown_user_onboarding_cannot_choose_alias" {
    title = "The user onboarding does not exist"

    when {
      title   = "Choose an alias for an unknown onboarding ID"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "HTTP 409 Conflict"
      error = "Could not choose user onboarding alias because of an unexpected state conflict, please retry"
    }
  }

  scenario "canceled_user_onboarding_cannot_choose_alias" {
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
      title = "HTTP 409 Conflict"
      error = "Could not choose user onboarding alias because user onboarding has been canceled"
    }
  }

  scenario "alias_is_claimed_concurrently" {
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
      title = "HTTP 409 Conflict"
      error = "Could not choose user onboarding alias because of a concurrency conflict, please retry"
    }

    comment {
      description = "Both requests read the alias as unclaimed. Another request commits first, so this request receives an optimistic concurrency conflict."
    }
  }
}

state_view "retrieve_claimed_alias" {
  title  = "Retrieve Claimed Alias"
  status = "done"

  readmodel "claimed_alias" {
    title    = "Claimed Alias"
    question = "Which user onboarding has claimed this alias?"
    from     = [event.onboarding.alias_claimed]

    field "id" {
      type         = "String"
      id_attribute = true
      example      = "joe-1984"
    }

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }

  scenario "alias_claim_is_projected" {
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
  title  = "Retrieve User Onboarding After Alias Chosen"
  status = "done"

  readmodel "user_onboarding" {
    title    = "User Onboarding"
    question = "What are the current onboarding details after an alias is chosen?"
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
    title = "Retrieve User Onboarding UI"
    actor = actor.authenticated_user
  }

  scenario "user_onboarding_shows_chosen_alias" {
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
  description = "Completing an existing onboarding creates a user ID and records the accepted terms version. Repeating completion returns the existing user ID without appending another event."
  status      = "done"

  screen "complete_user_onboarding_ui" {
    title = "Complete User Onboarding UI"
    actor = actor.authenticated_user
    to    = [command.complete_user_onboarding]

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
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }

    field "terms_and_conditions_version" {
      type    = "String"
      example = "1.0.0"
    }
  }

  scenario "user_onboarding_is_completed" {
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
      description = "The handler generates the user ID and appends User Onboarding Completed with the terms and conditions version. It does not check that an alias was chosen."
    }
  }

  scenario "unknown_user_onboarding_cannot_be_completed" {
    title = "The user onboarding does not exist"

    when {
      title   = "Complete an unknown onboarding ID"
      command = command.complete_user_onboarding
    }

    then {
      title = "HTTP 409 Conflict"
      error = "Could not complete user onboarding because of an unexpected state conflict, please retry"
    }
  }

  scenario "canceled_user_onboarding_cannot_be_completed" {
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
      title = "HTTP 409 Conflict"
      error = "Could not complete user onboarding because it has been canceled"
    }
  }

  scenario "user_onboarding_is_completed_concurrently" {
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
      title = "HTTP 409 Conflict"
      error = "Could not complete user onboarding because of a concurrency conflict, please retry"
    }

    comment {
      description = "The onboarding changes between the handler's read and append. An optimistic concurrency conflict is returned."
    }
  }
}

state_view "retrieve_user_onboarding_after_completion" {
  title  = "Retrieve User Onboarding After Completion"
  status = "done"

  readmodel "user_onboarding" {
    title    = "User Onboarding"
    question = "What are the completed onboarding details?"
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
    title = "Retrieve User Onboarding UI"
    actor = actor.authenticated_user
  }

  scenario "completed_user_onboarding_is_retrieved" {
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