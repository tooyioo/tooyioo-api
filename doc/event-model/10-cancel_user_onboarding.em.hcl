state_change "cancel_user_onboarding" {
  title       = "Cancel User Onboarding"
  description = "Cancel an in-progress onboarding, releasing its external identity and, when chosen, its alias. Completed or unknown onboardings cannot be canceled."
  status      = "done"

  screen "cancel_user_onboarding_ui" {
    title = "Cancel User Onboarding UI"
    actor = actor.authenticated_user
    to    = [command.cancel_user_onboarding]

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
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }

    field "reason" {
      type    = "String"
      example = "Changed mind"
    }
  }

  scenario "onboarding_without_alias_is_canceled" {
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

  scenario "onboarding_with_alias_is_canceled" {
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

  scenario "completed_onboarding_cannot_be_canceled" {
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

  scenario "unknown_onboarding_cannot_be_canceled" {
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
