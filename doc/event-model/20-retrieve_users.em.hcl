state_view "retrieve_users" {
  title       = "Retrieve Users"
  description = "Lists users from the shared Users projection using pagination and optional filters for identity, alias, name, email verification, and creation or modification dates. The projection is defined by Project Users in Chapter 1. The query is exposed as GET /users and is available independently of the onboarding profile screen. Each paginated item is a User object; its id is the user_id."
  status      = "done"

  readmodel "users" {
    title       = "Users"
    description = "Shared persisted Users read model populated by UserOnboardingCompleted. The projection is defined by Project Users in Chapter 1. This query returns paginated items containing user profile and identity details."
    question    = "Which users match the requested filters?"
    from        = [event.onboarding.user_onboarding_completed]
    to          = [screen.users_search]

    field "page_number" {
      type    = "Int"
      example = 1
    }

    field "page_size" {
      type    = "Int"
      example = 20
    }

    field "total_pages" {
      type    = "Int"
      example = 1
    }

    field "total_count" {
      type    = "Long"
      example = 1
    }

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

  screen "users_search" {
    title = "Search Users"
    actor = actor.authenticated_user

    field "page_number" {
      type    = "Int"
      example = 1
    }

    field "page_size" {
      type    = "Int"
      example = 20
    }

    field "total_pages" {
      type    = "Int"
      example = 1
    }

    field "total_count" {
      type    = "Long"
      example = 1
    }

    field "items" {
      type        = field_type.onboarding.user
      cardinality = "List"
    }
  }

  scenario "users_matching_filters_are_retrieved" {
    title = "The requested page of users is retrieved"

    given {
      title = "User profiles have been projected from completed onboardings"
      event = event.onboarding.user_onboarding_completed
    }

    then {
      title     = "The read model returns matching users and pagination details"
      readmodel = readmodel.users
    }
  }
}
