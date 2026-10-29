state_change "initiate_user_onboarding" {
  title       = "Initiate User Onboarding"
  description = "POST /user-onboarding/initiate starts an onboarding for an unclaimed external identity and claims it. It atomically writes the User Onboarding and External Identity Claiming streams. A retry with an already claimed identity returns the existing onboarding ID without appending events."
  status      = "done"

  screen "initiate_user_onboarding_ui" {
    title       = "Choose an Identity Provider"
    actor       = actor.authenticated_user
    to          = [command.initiate_user_onboarding]

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

  scenario "google_user_onboarding_is_initiated_with_unclaimed_external_identity" {
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

  scenario "external_identity_is_claimed_concurrently" {
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

  scenario "new_user_onboarding_id_is_not_empty" {
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

