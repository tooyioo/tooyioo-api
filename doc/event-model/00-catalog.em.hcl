# Actors and bounded-context contracts

board {
  workflows = [
    workflow.initiate_user_onboarding,
    workflow.retrieve_claimed_identity,
    workflow.retrieve_user_onboarding,
    workflow.choose_user_onboarding_alias,
    workflow.retrieve_claimed_alias,
    workflow.retrieve_user_onboarding_after_alias_chosen,
    workflow.complete_user_onboarding,
    workflow.project_users,
    workflow.retrieve_user_onboarding_after_completion,
    workflow.retrieve_user_profile,
    workflow.cancel_user_onboarding,
    workflow.retrieve_user_onboarding_after_cancellation,
    workflow.retrieve_users,
    workflow.retrieve_user_summaries,
  ]
}

actor "authenticated_user" {
  title         = "Authenticated User"
  auth_required = true
}

bounded_context "onboarding" {
  title = "User Onboarding"

  field_type "user" {
    type = "Custom"
  }

  field_type "user_summary" {
    type = "Custom"
  }

  aggregate "user_onboarding" {
    title       = "User Onboarding"
    description = "Stream of events associated to a user onboarding process."
  }

  aggregate "external_identity_claiming" {
    title       = "External Identity Claiming"
    description = "Stream of events associated to an external identity claiming process."
  }

  aggregate "alias_claiming" {
    title       = "Alias Claiming"
    description = "Stream of events associated to an alias claiming process."
  }

  event "user_onboarding_initiated" {
    title       = "User Onboarding Initiated"
    description = "A user onboarding process was initiated with a given external identity Id."
    aggregate   = aggregate.user_onboarding

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

  event "user_onboarding_alias_chosen" {
    title       = "User Onboarding Alias Chosen"
    description = "An alias was chosen for a given user onboarding."
    aggregate   = aggregate.user_onboarding

    field "alias" {
      type    = "String"
      example = "joe-1984"
    }
  }

  event "user_onboarding_completed" {
    title       = "User Onboarding Completed"
    description = "A given user onboarding is completed."
    aggregate   = aggregate.user_onboarding

    field "terms_and_conditions_version" {
      type    = "String"
      example = "1.0.0"
    }

    field "user_id" {
      type    = "String"
      example = "00000001-0000-0000-0000-000000000000"
    }
  }

  event "user_onboarding_canceled" {
    title       = "User Onboarding Canceled"
    description = "A user onboarding was canceled."
    aggregate   = aggregate.user_onboarding

    field "reason" {
      type    = "String"
      example = "Requested by user"
    }
  }

  event "external_identity_claimed" {
    title       = "External Identity Claimed"
    description = "An external identity was claimed."
    aggregate   = aggregate.external_identity_claiming

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }

  event "alias_claimed" {
    title       = "Alias Claimed"
    description = "An alias was claimed during user onboarding."
    aggregate   = aggregate.alias_claiming

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }


  event "external_identity_released" {
    title       = "External Identity Released"
    description = "An external identity was released when its onboarding was canceled."
    aggregate   = aggregate.external_identity_claiming

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }

  event "alias_released" {
    title       = "Alias Released"
    description = "An alias was released when its user onboarding was canceled."
    aggregate   = aggregate.alias_claiming

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }
}
