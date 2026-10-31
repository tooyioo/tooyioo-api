# Chapter 2 - Alternative: User Onboarding Cancellation workflows, in board order.

state_change "cancel_user_onboarding" {
  title       = "Cancel User Onboarding"
  description = "POST /user-onboarding/{id}/cancel cancels an in-progress onboarding. It writes the User Onboarding and External Identity Claiming streams, and also writes Alias Claiming when the onboarding has an alias. Completed or unknown onboardings cannot be canceled."
  status      = "done"

  screen "cancel_user_onboarding_ui" {
    title = "Cancel Onboarding"
    actor = actor.authenticated_user
    to    = [command.cancel_user_onboarding]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "reason" {
      type    = "String"
      example = "Changed mind"
    }
  }

  command "cancel_user_onboarding" {
    title       = "Cancel User Onboarding"
    description = "Cancels an in-progress user onboarding and releases its external identity and any alias claim."
    aggregate   = aggregate.onboarding.user_onboarding
    aggregate_dependencies = [
      aggregate.onboarding.external_identity_claiming,
      aggregate.onboarding.alias_claiming,
    ]
    to = [
      event.onboarding.user_onboarding_canceled,
      event.onboarding.external_identity_released,
      event.onboarding.alias_released,
    ]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "reason" {
      type    = "String"
      example = "Changed mind"
    }
  }

  scenario "scenario_01_onboarding_without_alias_is_canceled" {
    title = "Cancel before choosing an alias"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    given {
      title = "The external identity was claimed"
      event = event.onboarding.external_identity_claimed
    }

    when {
      title   = "Cancel the user onboarding"
      command = command.cancel_user_onboarding
    }

    then {
      title = "The user onboarding is canceled"
      event = event.onboarding.user_onboarding_canceled
    }

    then {
      title = "The external identity is released"
      event = event.onboarding.external_identity_released
    }
  }

  scenario "scenario_02_onboarding_with_alias_is_canceled" {
    title = "Cancel after choosing an alias"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    given {
      title = "The external identity was claimed"
      event = event.onboarding.external_identity_claimed
    }

    given {
      title = "An alias was chosen for the user onboarding"
      event = event.onboarding.user_onboarding_alias_chosen
    }

    given {
      title = "The alias was claimed"
      event = event.onboarding.alias_claimed
    }

    when {
      title   = "Cancel the user onboarding"
      command = command.cancel_user_onboarding
    }

    then {
      title = "The user onboarding is canceled"
      event = event.onboarding.user_onboarding_canceled
    }

    then {
      title = "The external identity is released"
      event = event.onboarding.external_identity_released
    }

    then {
      title = "The alias is released"
      event = event.onboarding.alias_released
    }

    comment {
      description = "The cancellation and both claim releases are appended atomically."
    }
  }

  scenario "scenario_03_completed_onboarding_cannot_be_canceled" {
    title = "A completed onboarding cannot be canceled"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    given {
      title = "The user onboarding was completed"
      event = event.onboarding.user_onboarding_completed
    }

    when {
      title   = "Attempt to cancel the completed onboarding"
      command = command.cancel_user_onboarding
    }

    then {
      title = "A completed onboarding remains completed and cannot be canceled"
      error = "onboarding_already_completed"
    }
  }

  scenario "scenario_04_unknown_onboarding_cannot_be_canceled" {
    title = "An onboarding that does not exist cannot be canceled"

    when {
      title   = "Attempt to cancel an unknown onboarding ID"
      command = command.cancel_user_onboarding
    }

    then {
      title = "No onboarding process exists to cancel"
      error = "onboarding_state_unexpected"
    }
  }
}

state_view "retrieve_user_onboarding_after_cancellation" {
  title       = "Retrieve User Onboarding After Cancellation"
  description = "Retrieves the persisted User Onboarding projection through GET /user-onboarding/{id}. The read model's id and the screen's user_onboarding_id identify the same onboarding."
  status      = "done"

  readmodel "user_onboarding" {
    title       = "User Onboarding"
    description = "Persistent User Onboarding read model assembled from onboarding events, including cancellation status and reason. It is exposed by GET /user-onboarding/{id} and returns one onboarding; it is not paginated."
    question    = "What is the state of this canceled user onboarding?"
    from = [
      event.onboarding.user_onboarding_initiated,
      event.onboarding.user_onboarding_alias_chosen,
      event.onboarding.user_onboarding_canceled,
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
      example = "canceled"
    }

    field "completed_at" {
      type     = "DateTime"
      optional = true
      example  = null
    }

    field "canceled_at" {
      type     = "DateTime"
      optional = true
      example  = "2026-01-01T00:10:00.123Z"
    }

    field "cancellation_reason" {
      type     = "String"
      optional = true
      example  = "Changed mind"
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
      example = "canceled"
    }

    field "completed_at" {
      type     = "DateTime"
      optional = true
      example  = null
    }

    field "canceled_at" {
      type     = "DateTime"
      optional = true
      example  = "2026-01-01T00:10:00.123Z"
    }

    field "cancellation_reason" {
      type     = "String"
      optional = true
      example  = "Changed mind"
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

  scenario "scenario_01_canceled_user_onboarding_without_alias_is_retrieved" {
    title = "Canceled onboarding view without an alias"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    given {
      title = "The user onboarding was canceled"
      event = event.onboarding.user_onboarding_canceled
    }

    then {
      title     = "The view shows canceled status and cancellation details"
      readmodel = readmodel.user_onboarding
    }
  }

  scenario "scenario_02_canceled_user_onboarding_with_alias_is_retrieved" {
    title = "Canceled onboarding view after choosing an alias"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    given {
      title = "An alias was chosen"
      event = event.onboarding.user_onboarding_alias_chosen
    }

    given {
      title = "The user onboarding was canceled"
      event = event.onboarding.user_onboarding_canceled
    }

    then {
      title     = "The view retains the alias and shows canceled status and cancellation details"
      readmodel = readmodel.user_onboarding
    }
  }
}
