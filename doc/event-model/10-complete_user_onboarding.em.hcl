state_change "complete_user_onboarding" {
  title       = "Complete User Onboarding"
  description = "POST /user-onboarding/{id}/complete completes an existing onboarding, creates a user ID, and records the accepted terms version. Repeating completion returns the existing user ID without appending another event."
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
      title = "No onboarding process exists to complete"
      error = "onboarding_state_unexpected"
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
      title = "A canceled onboarding cannot be completed"
      error = "onboarding_already_canceled"
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
      title = "This attempt conflicts because the onboarding changed before completion was saved"
      error = "concurrent_update"
    }

    comment {
      description = "This attempt returns a concurrency conflict. Retrying is safe: if another request completed the onboarding, the retry returns its existing user ID without appending another event; if cancellation won, the retry reports that the onboarding is canceled."
    }
  }
}
