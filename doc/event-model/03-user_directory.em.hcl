# Chapter 3 - User Directory workflows, in board order.

state_view "retrieve_users" {
  title       = "Retrieve Users"
  description = "GET /users lists users from the shared Users projection using pagination and optional filters for identity, alias, name, email verification, and creation or modification dates. The projection is defined by Project Users in Chapter 1 and only exposes completed users."
  status      = "done"

  readmodel "users" {
    title       = "Users"
    description = "Shared persisted Users read model populated by the user onboarding and personal data projections. It is exposed by GET /users with pagination and filters."
    question    = "Which users from the Users projection match the requested filters?"
    from = [
      event.user.user_personal_data_recorded,
      event.user.user_onboarding_alias_chosen,
      event.user.user_onboarding_completed,
      event.user.user_onboarding_canceled,
      event.user.user_personal_data_erasure_applied,
    ]
    to = [screen.users_search]

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
      type        = field_type.user.user
      example = [{
        id                           = "00000000-0000-0000-0000-000000000010"
        alias                        = "joe-bloggs-spain_123"
        name                         = "Joe"
        last_name                    = "Bloggs"
        email                        = "joe@test.com"
        is_email_verified            = true
        phone_number                 = "+34644000000"
        status                       = "completed"
        created_at                   = "2026-01-01T12:05:00.000Z"
        last_modified_at             = "2026-01-01T12:05:00.000Z"
        terms_of_service_version    = "1.0.0"
        privacy_policy_version      = "1.0.0"
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
      type        = field_type.user.user
    }
  }

  scenario "scenario_01_users_matching_filters_are_retrieved" {
    title = "The requested page of users is retrieved"

    given {
      title = "Users have been projected from completed user onboardings"
      event = event.user.user_onboarding_completed
    }

    then {
      title     = "The read model returns matching users and pagination details"
      readmodel = readmodel.users
    }
  }

  scenario "scenario_02_erased_users_are_not_listed" {
    title = "A user whose personal data was erased is not listed"

    given {
      title = "The user onboarding was completed"
      event = event.user.user_onboarding_completed
    }

    given {
      title = "The personal data erasure was applied"
      event = event.user.user_personal_data_erasure_applied
    }

    then {
      title     = "The read model does not list the erased user"
      readmodel = readmodel.users
    }
  }
}

state_view "retrieve_user_summaries" {
  title       = "Retrieve User Summaries"
  description = "GET /users/summaries returns a paginated, lightweight view of users from the same Users projection used by Retrieve Users. It filters by user ID or alias."
  status      = "done"

  readmodel "users" {
    title       = "Users"
    description = "Shared persisted Users read model used by GET /users/summaries with pagination and filters. It returns only user ID, alias, name, and last name."
    question    = "Which user summaries from the Users projection match the requested ID or alias?"
    from = [
      event.user.user_personal_data_recorded,
      event.user.user_onboarding_alias_chosen,
      event.user.user_onboarding_completed,
      event.user.user_onboarding_canceled,
      event.user.user_personal_data_erasure_applied,
    ]
    to = [screen.user_summaries_search]

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
      type        = field_type.user.user_summary
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
      type        = field_type.user.user_summary
    }
  }

  scenario "scenario_01_user_summaries_matching_filters_are_retrieved" {
    title = "The requested page of user summaries is retrieved"

    given {
      title = "Users have been projected from completed user onboardings"
      event = event.user.user_onboarding_completed
    }

    then {
      title     = "Matching user IDs, aliases, names, and pagination details are returned"
      readmodel = readmodel.users
    }
  }
}

state_view "retrieve_user_profile" {
  title       = "Retrieve User Profile"
  description = "GET /users/{user_id} returns the persisted User profile projection created after user onboarding completes. The User read model's id is the user_id."
  status      = "done"

  readmodel "user" {
    title       = "User"
    description = "Shared persisted User read model populated by the user onboarding and personal data projections. It is exposed by GET /users/{user_id} and returns one profile."
    question    = "What profile belongs to this user_id?"
    from = [
      event.user.user_personal_data_recorded,
      event.user.user_onboarding_alias_chosen,
      event.user.user_onboarding_completed,
      event.user.user_onboarding_canceled,
      event.user.user_personal_data_erasure_applied,
    ]
    to = [screen.user_profile]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = "joe-bloggs-spain_123"
    }

    field "name" {
      type     = "String"
      optional = true
      pii      = true
      example  = "Joe"
    }

    field "last_name" {
      type     = "String"
      optional = true
      pii      = true
      example  = "Bloggs"
    }

    field "email" {
      type     = "String"
      optional = true
      pii      = true
      example  = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "phone_number" {
      type    = "String"
      example = ""
    }

    field "status" {
      type    = "String"
      example = "completed"
    }

    field "created_at" {
      type    = "DateTime"
      example = "2026-01-01T12:05:00.000Z"
    }

    field "last_modified_at" {
      type    = "DateTime"
      example = "2026-01-01T12:05:00.000Z"
    }

    field "terms_of_service_version" {
      type     = "String"
      optional = true
      example  = "1.0.0"
    }

    field "privacy_policy_version" {
      type     = "String"
      optional = true
      example  = "1.0.0"
    }
  }

  screen "user_profile" {
    title = "User Profile"
    actor = actor.authenticated_user

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = "joe-bloggs-spain_123"
    }

    field "name" {
      type     = "String"
      optional = true
      example  = "Joe"
    }

    field "last_name" {
      type     = "String"
      optional = true
      example  = "Bloggs"
    }

    field "email" {
      type     = "String"
      optional = true
      example  = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "phone_number" {
      type    = "String"
      example = ""
    }

    field "status" {
      type    = "String"
      example = "completed"
    }
  }

  scenario "scenario_01_user_profile_is_retrieved" {
    title = "The user profile is retrieved"

    given {
      title = "The user onboarding was completed"
      event = event.user.user_onboarding_completed
    }

    then {
      title     = "The user profile is returned"
      readmodel = readmodel.user
    }
  }

  scenario "scenario_02_erased_user_profile_is_masked" {
    title = "The erased user profile is masked"

    given {
      title = "The user onboarding was completed"
      event = event.user.user_onboarding_completed
    }

    given {
      title = "The personal data erasure was applied"
      event = event.user.user_personal_data_erasure_applied
    }

    then {
      title     = "The user profile is returned with a masked identity"
      readmodel = readmodel.user
    }


  }
  scenario "scenario_03_unknown_user_profile_is_not_found" {
    title = "No user profile exists for the requested id"

    given {
      title = "A completed onboarding was not projected into the user read model"
      event = event.user.user_onboarding_completed
    }

    then {
      title = "The user profile cannot be retrieved"
      error = "profile_not_found"
    }
  }

}
