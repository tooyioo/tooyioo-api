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
      type        = field_type.onboarding.user_summary
      cardinality = "List"
      example = [{
        id        = "00000000-0000-0000-0000-000000000002"
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
      type        = field_type.onboarding.user_summary
      cardinality = "List"
    }
  }

  scenario "user_summaries_matching_filters_are_retrieved" {
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
