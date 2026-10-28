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
