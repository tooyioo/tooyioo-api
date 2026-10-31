state_view "retrieve_user_profile" {
  title       = "Retrieve User Profile"
  description = "After onboarding completes, the User projection creates a profile from the completed onboarding. The User read model's id is the user_id from UserOnboardingCompleted and is exposed as id by GET /users/{id}; the profile screen labels it user_id."
  status      = "done"

  readmodel "user" {
    title       = "User"
    description = "Shared persisted User read model populated by UserOnboardingCompleted. It is exposed by GET /users/{id} and returns one profile; it is not paginated."
    question = "What profile belongs to this user ID?"
    from     = [event.onboarding.user_onboarding_completed]
    to       = [screen.user_profile]

    field "id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000002"
    }

    field "alias" {
      type    = "String"
      example = "joe-bloggs-spain_123"
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

    field "phone_number" {
      type    = "String"
      example = ""
    }

    field "external_identity_id" {
      type    = "String"
      example = "google-sub-123"
    }

    field "external_identity_provider" {
      type    = "String"
      example = "Google"
    }

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }

    field "created_at" {
      type    = "DateTime"
      example = "2026-01-01T12:05:00.000Z"
    }

    field "last_modified_at" {
      type    = "DateTime"
      example = "2026-01-01T12:05:00.000Z"
    }

    field "terms_and_conditions_version" {
      type    = "String"
      example = "v1"
    }
  }

  screen "user_profile" {
    title = "User Profile"
    actor       = actor.authenticated_user

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000002"
    }

    field "alias" {
      type    = "String"
      example = "joe-bloggs-spain_123"
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

    field "phone_number" {
      type    = "String"
      example = ""
    }
  }

  scenario "completed_user_profile_is_retrieved" {
    title = "The user sees their profile after onboarding completes"

    given {
      title = "The user onboarding was completed and assigned a user ID"
      event = event.onboarding.user_onboarding_completed
    }

    then {
      title     = "The User read model contains the completed user's profile"
      readmodel = readmodel.user
    }

    comment {
      description = "The completion response supplies the user ID used to retrieve this profile. The User projector consumes UserOnboardingCompleted and retrieves the remaining profile details from the onboarding process."
    }
  }

  scenario "user_profile_is_not_available" {
    title = "No user profile is available for the requested ID"

    given {
      title = "The User read model does not contain the requested user"
      event = event.onboarding.user_onboarding_completed
    }

    then {
      title = "The user profile cannot be retrieved"
      error = "profile_not_found"
    }

    comment {
      description = "The profile query returns profile_not_found when the User document is absent, including while the completion event has not yet been projected."
    }
  }
}
