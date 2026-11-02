# Chapter 2 - Alternative: User Onboarding Cancellation workflows, in board order.

state_change "cancel_user_onboarding" {
  title       = "Cancel User Onboarding"
  description = "POST /user-onboarding/{user_onboarding_id}/cancel. Cancels an in-progress onboarding, releases its user login and alias, and requests an immediate personal data erasure. Completed or unknown user onboardings cannot be canceled."
  status      = "done"

  screen "cancel_user_onboarding_ui" {
    title = "Cancel User Onboarding"
    actor = actor.authenticated_user
    to    = [command.cancel_user_onboarding]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "reason_code" {
      type    = "String"
      example = "changed_mind"
    }
  }

  command "cancel_user_onboarding" {
    title       = "Cancel User Onboarding"
    description = "Cancels an in-progress onboarding, releases its user login and alias, and requests an immediate personal data erasure in one atomic multi-stream append."
    aggregate   = aggregate.user.user_onboarding
    api_endpoint = "POST /user-onboarding/{user_onboarding_id}/cancel"

    aggregate_dependencies = [
      aggregate.user.user_login_claiming,
      aggregate.user.user_login_linking,
      aggregate.user.user_alias_claiming,
      aggregate.user.user_personal_data,
    ]

    to = [
      event.user.user_onboarding_canceled,
      event.user.user_login_released,
      event.user.user_login_unlinked,
      event.user.alias_released,
      event.user.user_personal_data_erasure_requested,
    ]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "reason_code" {
      type    = "String"
      example = "changed_mind"
    }
  }

  scenario "scenario_01_user_onboarding_without_alias_is_canceled" {
    title = "Cancel before choosing an alias"

    given {
      title = "The user onboarding was initiated"
      event = event.user.user_onboarding_initiated
    }

    given {
      title = "The user login was claimed"
      event = event.user.user_login_claimed
    }

    given {
      title = "The user login is linked to the user"
      event = event.user.user_login_linked
    }

    when {
      title   = "Cancel the user onboarding"
      command = command.cancel_user_onboarding
    }

    then {
      title = "The user onboarding is canceled"
      event = event.user.user_onboarding_canceled
    }

    then {
      title = "The user login is released"
      event = event.user.user_login_released
    }

    then {
      title = "The user login is unlinked"
      event = event.user.user_login_unlinked
    }

    then {
      title = "The personal data erasure is requested"
      event = event.user.user_personal_data_erasure_requested
    }

    comment {
      description = "The cancellation, the user login release/unlink and the erasure request are appended atomically. The erasure request has an immediate due date; the scheduled erasure processor applies it."
    }
  }

  scenario "scenario_02_user_onboarding_with_alias_is_canceled" {
    title = "Cancel after choosing an alias"

    given {
      title = "The user onboarding was initiated"
      event = event.user.user_onboarding_initiated
    }

    given {
      title = "The user login was claimed"
      event = event.user.user_login_claimed
    }

    given {
      title = "The user login is linked to the user"
      event = event.user.user_login_linked
    }

    given {
      title = "An alias was chosen for the user onboarding"
      event = event.user.user_onboarding_alias_chosen
    }

    given {
      title = "The alias was claimed"
      event = event.user.alias_claimed
    }

    when {
      title   = "Cancel the user onboarding"
      command = command.cancel_user_onboarding
    }

    then {
      title = "The user onboarding is canceled"
      event = event.user.user_onboarding_canceled
    }

    then {
      title = "The user login is released"
      event = event.user.user_login_released
    }

    then {
      title = "The user login is unlinked"
      event = event.user.user_login_unlinked
    }

    then {
      title = "The alias is released"
      event = event.user.alias_released
    }

    then {
      title = "The personal data erasure is requested"
      event = event.user.user_personal_data_erasure_requested
    }

    comment {
      description = "The cancellation, the user login release/unlink and the alias release and the erasure request are appended atomically. Repeating the public request is not idempotent because the released user login no longer resolves the user."
    }
  }

  scenario "scenario_03_completed_user_onboarding_cannot_be_canceled" {
    title = "A completed user onboarding cannot be canceled"

    given {
      title = "The user onboarding was initiated"
      event = event.user.user_onboarding_initiated
    }

    given {
      title = "The user onboarding was completed"
      event = event.user.user_onboarding_completed
    }

    when {
      title   = "Attempt to cancel the completed user onboarding"
      command = command.cancel_user_onboarding
    }

    then {
      title = "A completed user onboarding remains completed and cannot be canceled"
      error = "onboarding_already_completed"
    }
  }

  scenario "scenario_04_unknown_user_onboarding_cannot_be_canceled" {
    title = "A user onboarding that does not exist cannot be canceled"

    when {
      title   = "Attempt to cancel an unknown user_id"
      command = command.cancel_user_onboarding
    }

    then {
      title = "No user onboarding process exists to cancel"
      error = "onboarding_state_unexpected"
    }
  }

  scenario "scenario_05_user_onboarding_is_canceled_concurrently" {
    title = "Another request changes the onboarding first"

    given {
      title = "The user onboarding was initiated"
      event = event.user.user_onboarding_initiated
    }

    when {
      title   = "Cancel the user onboarding while another request changes it"
      command = command.cancel_user_onboarding
    }

    then {
      title = "This attempt conflicts because the onboarding changed before cancellation was saved"
      error = "concurrent_update"
    }
  }
}
