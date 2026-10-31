state_view "project_users" {
  title       = "Project Users"
  description = "Projects UserOnboardingCompleted into the shared Users read model consumed by the User Directory queries in Chapter 3. This slice makes the projection and its source event explicit."
  status      = "done"

  readmodel "users" {
    title       = "Users"
    description = "Shared persisted User read model populated by UserOnboardingCompleted. The User Directory queries in Chapter 3 read this projection."
    question    = "Which users have been projected from completed onboardings?"
    from        = [event.onboarding.user_onboarding_completed]

    field "items" {
      type        = field_type.onboarding.user
      cardinality = "List"
      example = [{
        id                          = "00000000-0000-0000-0000-000000000002"
        alias                       = "joe-bloggs-spain_123"
        name                        = "Joe"
        last_name                   = "Bloggs"
        email                       = "joe@test.com"
        is_email_verified           = true
        phone_number                = "+34644000000"
        external_id                 = "google-sub-123"
        external_provider           = "Google"
        user_onboarding_id          = "00000000-0000-0000-0000-000000000001"
        created_at                  = "2026-01-01T12:05:00.000Z"
        last_modified_at            = "2026-01-01T12:05:00.000Z"
        terms_and_conditions_version = "v1"
      }]
    }
  }

  scenario "users_are_projected_after_completion" {
    title = "Users are projected after an onboarding completes"

    given {
      title = "The user onboarding was completed"
      event = event.onboarding.user_onboarding_completed
    }

    then {
      title     = "The Users read model contains the completed user"
      readmodel = readmodel.users
    }

    comment {
      description = "The projection is shared by the User Directory queries in Chapter 3. Those queries declare their own Users read model shape for pagination and filtering."
    }
  }
}
