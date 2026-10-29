state_view "retrieve_user_onboarding_after_completion" {
  title       = "Retrieve User Onboarding After Completion"
  description = "Retrieves the persisted User Onboarding projection through GET /user-onboarding/{id}. The read model's id and the screen's user_onboarding_id identify the same onboarding."
  status      = "done"

  readmodel "user_onboarding" {
    title       = "User Onboarding"
    description = "Persistent User Onboarding read model assembled from onboarding events."
    question    = "What are the completed onboarding details?"
    from = [
      event.onboarding.user_onboarding_initiated,
      event.onboarding.user_onboarding_alias_chosen,
      event.onboarding.user_onboarding_completed,
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
      example = "completed"
    }

    field "completed_at" {
      type     = "DateTime"
      optional = true
      example  = "2026-01-01T00:10:00.123Z"
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
      example = "2026-01-01T00:10:00.123Z"
    }

    field "revision" {
      type    = "Long"
      example = 3
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
      example = "completed"
    }

    field "completed_at" {
      type     = "DateTime"
      optional = true
      example  = "2026-01-01T00:10:00.123Z"
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
      example = "2026-01-01T00:10:00.123Z"
    }
  }

  scenario "completed_user_onboarding_is_retrieved" {
    title = "Completed user onboarding view"

    given {
      title = "The user onboarding was initiated"
      event = event.onboarding.user_onboarding_initiated
    }

    given {
      title = "An alias was chosen"
      event = event.onboarding.user_onboarding_alias_chosen
    }

    given {
      title = "The user onboarding was completed"
      event = event.onboarding.user_onboarding_completed
    }

    then {
      title     = "The view shows completed status and completion time"
      readmodel = readmodel.user_onboarding
    }

    comment {
      description = "This is the same User Onboarding document after its completion event. The chosen alias is present in this example, although the completion handler does not require it."
    }
  }
}
