state_view "retrieve_user_onboarding_after_alias_chosen" {
  title       = "Retrieve User Onboarding After Alias Chosen"
  description = "Retrieves the persisted User Onboarding projection through GET /user-onboarding/{id}. The read model's id and the screen's user_onboarding_id identify the same onboarding."
  status      = "done"

  readmodel "user_onboarding" {
    title       = "User Onboarding"
    description = "Persistent User Onboarding read model assembled from onboarding events. It is exposed by GET /user-onboarding/{id} and returns one onboarding; it is not paginated."
    question    = "What are the current onboarding details after an alias is chosen?"
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
  }

  scenario "scenario_01_user_onboarding_shows_chosen_alias" {
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
