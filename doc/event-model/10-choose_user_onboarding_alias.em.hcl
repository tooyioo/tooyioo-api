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
      title = "The alias remains claimed by another onboarding and cannot be chosen"
      error = "alias_already_claimed"
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
      title = "The onboarding keeps its existing alias; the requested replacement is rejected"
      error = "alias_already_chosen"
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
      title = "No onboarding process exists to choose an alias for"
      error = "onboarding_state_unexpected"
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
      title = "An onboarding that is no longer in progress cannot be assigned an alias"
      error = "onboarding_not_in_progress"
    }
  }

  scenario "completed_user_onboarding_cannot_choose_alias" {
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
      title = "This attempt conflicts because the alias claim changed before it was saved"
      error = "concurrent_update"
    }

    comment {
      description = "This attempt returns a concurrency conflict. Retrying is safe: if the same onboarding claimed the alias, the retry succeeds without another event; if another onboarding claimed it, the retry reports that the alias is in use."
    }
  }
}
