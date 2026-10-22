# Actors

actor "anonymous_user" {
  title         = "User"
  auth_required = false
}

actor "authenticated_user" {
  title         = "User"
  auth_required = true
}

# Bounded Context

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

  event "user_onboarding_initiated" {
    title       = "User Onboarding Initiated"
    description = "A user onboarding process was initiated with a given external identity Id."

    aggregate = aggregate.user_onboarding

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

  event "external_identity_claimed" {
    title       = "External Identity Claimed"
    description = "An external identity was claimed during user onboarding."

    aggregate = aggregate.external_identity_claiming

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }
}

# Chapters

chapter "user_onboarding" {
  title     = "Chapter 1 - User Onboarding"
  workflows = [
    workflow.initiate_user_onboarding,
    workflow.retrieve_user_onboarding,
  ]
}

# Slices

state_change "initiate_user_onboarding" {
  title  = "Initiate User Onboarding"
  status = "done"

  screen "initiate_user_onboarding_ui" {
    title = "Initiate User Onboarding UI"
    actor = actor.anonymous_user

    field "provider" {
      type    = "String"
      example = "Google"
    }

    to    = [command.initiate_user_onboarding]
  }

  screen_image "initiate_user_onboarding_mockup" {
    url = "./images/initiate-user-onboarding.png"
  }

  command "initiate_user_onboarding" {
    title       = "Initiate User Onboarding"
    description = "Initiates onboarding with the user's details retrieved from the external provider's id_token. It's idempotent."

    aggregate = aggregate.onboarding.user_onboarding

    aggregate_dependencies = [
      aggregate.onboarding.external_identity_claiming,
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

    field "is_email_confirmed" {
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

    to = [
      event.onboarding.user_onboarding_initiated,
      event.onboarding.external_identity_claimed,
    ]
  }

  scenario "google_user_onboarding_is_initiated_with_unclaimed_external_identity" {
    when {
      command = command.initiate_user_onboarding
    }

    then {
      event = event.onboarding.user_onboarding_initiated
    }

    then {
      event = event.onboarding.external_identity_claimed
    }

    comment {
      description = "When a user starts onboarding with an unclaimed Google identity, the system records the onboarding details and claims the external identity."
    }
  }
}

state_view "retrieve_user_onboarding" {
  title  = "Retrieve User Onboarding"
  status = "done"

  readmodel "user_onboarding" {
    title    = "User Onboarding"
    question = "What is the current state of this user onboarding?"

    description = "Current user onboarding details assembled from the onboarding events."

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
      example = null
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
      type = "String"
      example = "in_progress"
    }

    field "completed_at" {
      type     = "DateTime"
      optional = true
      example = null
    }

    field "canceled_at" {
      type     = "DateTime"
      optional = true
      example = null
    }

    field "cancellation_reason" {
      type     = "String"
      optional = true
      example = null
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
    
    from = [
      event.onboarding.user_onboarding_initiated
    ]

    to = [screen.retrieve_user_onboarding_ui]
  }

  screen "retrieve_user_onboarding_ui" {
    title = "Retrieve User Onboarding UI"
    actor = actor.authenticated_user
  }

  scenario "user_onboarding_is_retrieved" {
    given {
      event = event.onboarding.user_onboarding_initiated
    }

    then {
      readmodel = readmodel.user_onboarding
    }

    comment {
      description = "The read model exposes the user onboarding details and current status."
    }
  }
}