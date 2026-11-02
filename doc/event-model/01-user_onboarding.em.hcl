# Chapter 1 - User Onboarding workflows, in board order.

state_change "initiate_user_onboarding" {
  title       = "Initiate User Onboarding"
  description = "POST /user-onboarding/initiate. Resolves the user login, mints a user_onboarding_id and a user_id, records the person's PII, links and claims the user login, and starts the onboarding process in one atomic multi-stream append. A retry with an already claimed user login returns the existing ids and appends nothing."
  status      = "done"

  screen "initiate_user_onboarding_ui" {
    title = "Initiate User Onboarding"
    actor = actor.authenticated_user
    to    = [command.initiate_user_onboarding]
  }

  command "initiate_user_onboarding" {
    title       = "Initiate User Onboarding"
    description = "Generates the user_onboarding_id and user_id in the mapper, derives user_login_key = HMAC(pepper, kind|issuer|subject) and writes personal data, the user login link, the user login claim and the onboarding initiation atomically."
    aggregate   = aggregate.user.user_onboarding
    api_endpoint = "POST /user-onboarding/initiate"

    aggregate_dependencies = [
      aggregate.user.user_personal_data,
      aggregate.user.user_login_linking,
      aggregate.user.user_login_claiming,
    ]

    to = [
      event.user.user_personal_data_recorded,
      event.user.user_login_linked,
      event.user.user_login_claimed,
      event.user.user_onboarding_initiated,
    ]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
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

    field "user_login_key" {
      type    = "String"
      example = "5f0c2e..."
    }

    field "kind" {
      type    = "String"
      example = "oidc"
    }

    field "issuer" {
      type    = "String"
      example = "https://accounts.google.com"
    }
  }

  scenario "scenario_01_user_onboarding_is_initiated_with_an_unclaimed_user_login" {
    title = "Start user onboarding with an unclaimed user login"

    when {
      title   = "Initiate user onboarding with the identity returned by Google"
      command = command.initiate_user_onboarding
    }

    then {
      title = "The personal data is recorded"
      event = event.user.user_personal_data_recorded
    }

    then {
      title = "The user login is linked to the user"
      event = event.user.user_login_linked
    }

    then {
      title = "The user login is claimed"
      event = event.user.user_login_claimed
    }

    then {
      title = "The user onboarding is initiated"
      event = event.user.user_onboarding_initiated
    }

    comment {
      description = "All four streams are appended atomically with expected versions, so a person never exists without personal data, a user login link and an onboarding process."
    }
  }

  scenario "scenario_03_released_user_login_is_claimed_again" {
    title = "A released user login starts a new user"

    given {
      title = "The user login was claimed by a previous user"
      event = event.user.user_login_claimed
    }

    given {
      title = "The user login was released"
      event = event.user.user_login_released
    }

    when {
      title   = "Initiate user onboarding with the released user login"
      command = command.initiate_user_onboarding
    }

    then {
      title = "The personal data is recorded for the new user"
      event = event.user.user_personal_data_recorded
    }

    then {
      title = "The user login is linked to the new user"
      event = event.user.user_login_linked
    }

    then {
      title = "The user login is claimed for the new user"
      event = event.user.user_login_claimed
    }

    then {
      title = "The user onboarding is initiated"
      event = event.user.user_onboarding_initiated
    }

    comment {
      description = "Re-registration after a cancellation or erasure mints a new user_id. The previous onboarding streams remain as closed pseudonyms."
    }
  }

  scenario "scenario_04_user_login_is_claimed_concurrently" {
    title = "Another request claims the user login first"

    when {
      title   = "Initiate another user onboarding for the given user login"
      command = command.initiate_user_onboarding
    }

    then {
      title = "This attempt conflicts because another request claimed the user login first"
      error = "concurrent_update"
    }

    comment {
      description = "Retrying is safe: once the committed claim is visible, the same user login returns its existing user_id without appending events."
    }
  }

  scenario "scenario_05_generated_user_id_already_has_events" {
    title = "The generated user_id already has events"

    when {
      title   = "Initiate user onboarding with a generated user_id that already has events"
      command = command.initiate_user_onboarding
    }

    then {
      title = "The generated user_id is already associated with an existing process"
      error = "onboarding_state_unexpected"
    }
  }
}

state_view "retrieve_claimed_user_login" {
  title       = "Retrieve Claimed User Login"
  description = "Internal lookup; not an HTTP endpoint. The token filter derives the user login key, reads this state and adds the user_id claim. A released user login resolves to no user."
  status      = "done"

  readmodel "claimed_user_login" {
    title       = "Claimed User Login"
    description = "In-memory command-side state rehydrated from the user login claiming stream."
    question    = "Which user_id, if any, is this user login claimed by?"

    from = [
      event.user.user_login_claimed,
      event.user.user_login_released,
    ]

    field "user_login_key" {
      type         = "String"
      id_attribute = true
      example      = "5f0c2e..."
    }

    field "user_id" {
      type     = "String"
      optional = true
      example  = "00000000-0000-0000-0000-000000000010"
    }

    field "user_onboarding_id" {
      type     = "String"
      optional = true
      example  = "00000000-0000-0000-0000-000000000001"
    }

    field "is_claimed" {
      type    = "Boolean"
      example = true
    }
  }
}

state_view "retrieve_linked_user_logins" {
  title       = "Retrieve Linked User Logins"
  description = "Internal lookup; not an HTTP endpoint. Command-side state used by cancellation and erasure to release every user login, and by a future link/unlink slice. It is in memory, like the claimed user login state; it does not need a Mongo read model."
  status      = "done"

  readmodel "linked_user_logins" {
    title       = "Linked User Logins"
    description = "In-memory command-side state rehydrated from the user login linking stream."
    question    = "Which user logins are linked to this user?"

    from = [
      event.user.user_login_linked,
      event.user.user_login_unlinked,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "logins" {
      cardinality = "List"
      type        = field_type.user.linked_user_login
      example = [{
        user_login_key = "5f0c2e..."
        kind           = "oidc"
        issuer         = "https://accounts.google.com"
      }]
    }
  }
}

state_view "retrieve_user_onboarding" {
  title       = "Retrieve User Onboarding"
  description = "GET /user-onboarding/{user_onboarding_id} reads the user onboarding state directly from the stream and joins the User Personal Data state. It is not a Mongo projection."
  status      = "done"

  readmodel "user_onboarding" {
    title       = "User Onboarding"
    description = "In-memory User Onboarding state view rehydrated from the onboarding process stream. No PII."
    question    = "What is the current state of this user onboarding?"
    from = [
      event.user.user_onboarding_initiated,
      event.user.user_onboarding_alias_chosen,
      event.user.user_onboarding_completed,
      event.user.user_onboarding_canceled,
    ]
    to = [screen.retrieve_user_onboarding_ui]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
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

    field "cancellation_reason_code" {
      type     = "String"
      optional = true
      example  = "changed_mind"
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

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = "joe-1984"
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

    field "status" {
      type    = "String"
      example = "completed"
    }
  }

  scenario "scenario_01_completed_user_onboarding_is_retrieved" {
    title = "Completed user onboarding view"

    given {
      title = "The user onboarding was initiated"
      event = event.user.user_onboarding_initiated
    }

    given {
      title = "An alias was chosen"
      event = event.user.user_onboarding_alias_chosen
    }

    given {
      title = "The user onboarding was completed"
      event = event.user.user_onboarding_completed
    }

    then {
      title     = "The view shows completed status and completion time"
      readmodel = readmodel.user_onboarding
    }
  }

  scenario "scenario_02_canceled_user_onboarding_is_retrieved" {
    title = "Canceled user onboarding view"

    given {
      title = "The user onboarding was canceled"
      event = event.user.user_onboarding_canceled
    }

    then {
      title     = "The view shows canceled status and a masked identity"
      readmodel = readmodel.user_onboarding
    }
  }

  scenario "scenario_03_unknown_user_onboarding_is_not_found" {
    title = "No user onboarding state exists for the requested id"

    given {
      title = "A user login exists but no user onboarding was initiated"
      event = event.user.user_login_claimed
    }

    then {
      title = "The user onboarding cannot be retrieved"
      error = "user_onboarding_not_found"
    }
  }

}



state_change "choose_user_onboarding_alias" {
  title       = "Choose User Onboarding Alias"
  description = "POST /user-onboarding/{user_onboarding_id}/choose-alias. Claims an alias for the user. The alias is 3-30 lowercase letters, numbers, dashes or underscores. Choosing the same alias again is idempotent; choosing an alias owned by another user is rejected."
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
    description = "Writes a 3-30 character lowercase alias on the onboarding process and claims it for the user in one atomic append."
    aggregate   = aggregate.user.user_onboarding
    api_endpoint = "POST /user-onboarding/{user_onboarding_id}/choose-alias"

    aggregate_dependencies = [
      aggregate.user.user_alias_claiming,
    ]

    to = [
      event.user.user_onboarding_alias_chosen,
      event.user.alias_claimed,
    ]

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

  scenario "scenario_01_alias_is_available_and_is_claimed" {
    title = "Choose an available alias"

    given {
      title = "The user onboarding was initiated"
      event = event.user.user_onboarding_initiated
    }

    when {
      title   = "Choose an available alias"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "The alias is chosen"
      event = event.user.user_onboarding_alias_chosen
    }

    then {
      title = "The alias is claimed"
      event = event.user.alias_claimed
    }

    comment {
      description = "The onboarding and alias claim streams are appended atomically with expected versions."
    }
  }

  scenario "scenario_03_a_different_alias_cannot_replace_the_chosen_one" {
    title = "A different alias is chosen after one was already chosen"

    given {
      title = "The alias was already chosen"
      event = event.user.user_onboarding_alias_chosen
    }

    when {
      title   = "Choose a different alias"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "The existing alias is kept and the different alias is rejected"
      error = "alias_already_chosen"
    }
  }

  scenario "scenario_04_alias_is_already_claimed_by_another_user" {
    title = "The alias is owned by another user"

    given {
      title = "The user onboarding was initiated"
      event = event.user.user_onboarding_initiated
    }

    given {
      title = "Another user claimed the alias"
      event = event.user.alias_claimed
    }

    when {
      title   = "Choose the alias owned by another user"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "The alias claim points to another user"
      error = "alias_already_claimed"
    }
  }

  scenario "scenario_05_alias_cannot_be_chosen_outside_an_in_progress_onboarding" {
    title = "The onboarding is completed or canceled"

    given {
      title = "The user onboarding was canceled"
      event = event.user.user_onboarding_canceled
    }

    when {
      title   = "Choose an alias"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "The alias cannot be chosen because the onboarding is no longer in progress"
      error = "onboarding_not_in_progress"
    }
  }

  scenario "scenario_06_unknown_user_onboarding_cannot_choose_an_alias" {
    title = "The user onboarding does not exist"

    when {
      title   = "Choose an alias for an unknown user_id"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "No user onboarding process exists to choose an alias for"
      error = "onboarding_state_unexpected"
    }
  }

  scenario "scenario_07_the_alias_is_chosen_concurrently" {
    title = "Another request changes the onboarding first"

    given {
      title = "The user onboarding was initiated"
      event = event.user.user_onboarding_initiated
    }

    when {
      title   = "Choose an alias while another request changes the onboarding"
      command = command.choose_user_onboarding_alias
    }

    then {
      title = "This attempt conflicts because the onboarding changed before the alias was saved"
      error = "concurrent_update"
    }
  }
}

state_view "retrieve_claimed_alias" {
  title       = "Retrieve Claimed Alias"
  description = "Internal lookup; not an HTTP endpoint. Command-side state rehydrated from the User Alias Claiming stream."
  status      = "done"

  readmodel "claimed_alias" {
    title       = "Claimed Alias"
    description = "In-memory command-side state rehydrated from the User Alias Claiming stream."
    question    = "Which user_id, if any, currently owns this alias?"

    from = [
      event.user.alias_claimed,
      event.user.alias_released,
    ]

    field "alias" {
      type         = "String"
      id_attribute = true
      example      = "joe-1984"
    }

    field "user_id" {
      type     = "String"
      optional = true
      example  = "00000000-0000-0000-0000-000000000010"
    }
  }
}

state_change "complete_user_onboarding" {
  title       = "Complete User Onboarding"
  description = "POST /user-onboarding/{user_onboarding_id}/complete. Completes an onboarding that already has an alias and records the accepted terms of service and privacy policy versions. No id is minted here: both ids already exist."
  status      = "done"

  screen "complete_user_onboarding_ui" {
    title = "Complete User Onboarding"
    actor = actor.authenticated_user
    to    = [command.complete_user_onboarding]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "terms_of_service_version" {
      type    = "String"
      example = "1.0.0"
    }

    field "privacy_policy_version" {
      type    = "String"
      example = "1.0.0"
    }
  }

  command "complete_user_onboarding" {
    title       = "Complete User Onboarding"
    description = "Appends UserOnboardingCompleted to the process stream with the accepted terms of service and privacy policy versions. Consent evidence must survive an erasure."
    aggregate   = aggregate.user.user_onboarding
    api_endpoint = "POST /user-onboarding/{user_onboarding_id}/complete"

    to = [
      event.user.user_onboarding_completed,
    ]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "terms_of_service_version" {
      type    = "String"
      example = "1.0.0"
    }

    field "privacy_policy_version" {
      type    = "String"
      example = "1.0.0"
    }
  }

  scenario "scenario_01_user_onboarding_is_completed" {
    title = "Complete an onboarding with a chosen alias"

    given {
      title = "The user onboarding was initiated"
      event = event.user.user_onboarding_initiated
    }

    given {
      title = "An alias was chosen"
      event = event.user.user_onboarding_alias_chosen
    }

    when {
      title   = "Complete the user onboarding"
      command = command.complete_user_onboarding
    }

    then {
      title = "The user onboarding is completed"
      event = event.user.user_onboarding_completed
    }
  }

  scenario "scenario_02_user_onboarding_without_alias_cannot_be_completed" {
    title = "Complete an onboarding without an alias"

    given {
      title = "The user onboarding was initiated"
      event = event.user.user_onboarding_initiated
    }

    when {
      title   = "Complete the user onboarding"
      command = command.complete_user_onboarding
    }

    then {
      title = "The onboarding cannot be completed without an alias"
      error = "onboarding_alias_not_chosen"
    }
  }

  scenario "scenario_03_canceled_user_onboarding_cannot_be_completed" {
    title = "Complete a canceled onboarding"

    given {
      title = "The user onboarding was canceled"
      event = event.user.user_onboarding_canceled
    }

    when {
      title   = "Complete the canceled user onboarding"
      command = command.complete_user_onboarding
    }

    then {
      title = "The canceled onboarding remains canceled"
      error = "onboarding_already_canceled"
    }
  }

  scenario "scenario_05_unknown_user_onboarding_cannot_be_completed" {
    title = "The user onboarding does not exist"

    when {
      title   = "Complete an unknown user_id"
      command = command.complete_user_onboarding
    }

    then {
      title = "No user onboarding process exists to complete"
      error = "onboarding_state_unexpected"
    }
  }

  scenario "scenario_06_user_onboarding_is_completed_concurrently" {
    title = "Another request changes the onboarding first"

    given {
      title = "The user onboarding was initiated"
      event = event.user.user_onboarding_initiated
    }

    when {
      title   = "Complete the user onboarding while another request changes it"
      command = command.complete_user_onboarding
    }

    then {
      title = "This attempt conflicts because the onboarding changed before completion was saved"
      error = "concurrent_update"
    }
  }
}

state_view "project_users" {
  title       = "Project Users"
  description = "Internal projection; not an HTTP endpoint. Projects the user's personal data copy, alias and onboarding completion into the shared Users read model consumed by the directory queries."
  status      = "done"

  readmodel "users" {
    title       = "Users"
    description = "Persisted Users read model consumed by the directory queries. Holds a copy of the PII for search and anonymizes it on UserPersonalDataErasureApplied."
    question    = "Which users match the requested filters?"

    from = [
      event.user.user_personal_data_recorded,
      event.user.user_onboarding_alias_chosen,
      event.user.user_onboarding_completed,
      event.user.user_onboarding_canceled,
      event.user.user_personal_data_erasure_applied,
    ]

    field "user_onboarding_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000001"
    }

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = "joe-1984"
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

    field "phone_number" {
      type    = "String"
      example = ""
    }

    field "status" {
      type    = "String"
      example = "completed"
    }

    field "terms_of_service_version" {
      type     = "String"
      optional = true
      example  = "1.0.0"
    }

    field "privacy_policy_version" {
      type     = "String"
      optional = true
      example  = "1.0.0"
    }

    field "created_at" {
      type    = "DateTime"
      example = "2026-11-01T00:00:00.000Z"
    }

    field "last_modified_at" {
      type    = "DateTime"
      example = "2026-11-01T00:10:00.000Z"
    }

    field "revision" {
      type    = "Long"
      example = 3
    }
  }
}
