# Chapter 3 - User Directory workflows, in board order.

state_view "retrieve_users" {
  title       = "Retrieve Users"
  description = "Lists users from the shared Users projection using pagination and optional filters for identity, alias, name, email verification, and creation or modification dates. The projection is defined by Project Users in Chapter 1. The query is exposed as GET /users and is available independently of the onboarding profile screen. Each paginated item is a User object; its id is the user_id."
  status      = "done"

  readmodel "users" {
    title       = "Users"
    description = "Shared persisted Users read model populated by UserOnboardingCompleted. The projection is defined by Project Users in Chapter 1. It is exposed by GET /users with pagination and filters and returns user profile and identity details."
    question    = "Which users from the UserOnboardingCompleted projection match the requested filters?"
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
      cardinality = "List"
      type        = field_type.onboarding.user
      example = [{
        id                           = "00000000-0000-0000-0000-000000000010"
        alias                        = "joe-bloggs-spain_123"
        name                         = "Joe"
        last_name                    = "Bloggs"
        email                        = "joe@test.com"
        is_email_verified            = true
        phone_number                 = "+34644000000"
        external_id                  = "google-sub-123"
        external_provider            = "Google"
        user_onboarding_id           = "00000000-0000-0000-0000-000000000001"
        created_at                   = "2026-01-01T12:05:00.000Z"
        last_modified_at             = "2026-01-01T12:05:00.000Z"
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
      cardinality = "List"
      type        = field_type.onboarding.user
    }
  }

  scenario "scenario_01_users_matching_filters_are_retrieved" {
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

state_view "retrieve_user_summaries" {
  title       = "Retrieve User Summaries"
  description = "Returns a paginated, lightweight view of users from the same Users projection used by Retrieve Users. The projection is defined by Project Users in Chapter 1. It filters by user ID or alias and is exposed as GET /users/summaries. Each paginated User Summary item's id is the user_id."
  status      = "done"

  readmodel "users" {
    title       = "Users"
    description = "Shared persisted Users read model populated by UserOnboardingCompleted. The projection is defined by Project Users in Chapter 1. It is exposed by GET /users/summaries with pagination and filters and returns only user ID, alias, name, and last name."
    question    = "Which user summaries from the UserOnboardingCompleted projection match the requested ID or alias?"
    from        = [event.onboarding.user_onboarding_completed]
    to          = [screen.user_summaries_search]

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
      cardinality = "List"
      type        = field_type.onboarding.user_summary
      example = [{
        id        = "00000000-0000-0000-0000-000000000010"
        alias     = "joe-bloggs-spain_123"
        name      = "Joe"
        last_name = "Bloggs"
      }]
    }
  }

  screen "user_summaries_search" {
    title = "Search User Summaries"
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
      cardinality = "List"
      type        = field_type.onboarding.user_summary
    }
  }

  scenario "scenario_01_user_summaries_matching_filters_are_retrieved" {
    title = "The requested page of user summaries is retrieved"

    given {
      title = "User profiles have been projected from completed onboardings"
      event = event.onboarding.user_onboarding_completed
    }

    then {
      title     = "Matching user IDs, aliases, names, and pagination details are returned"
      readmodel = readmodel.users
    }
  }
}
