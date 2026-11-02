# Actors and bounded-context contracts

board {
  workflows = [
    workflow.initiate_user_onboarding,
    workflow.retrieve_claimed_user_login,
    workflow.retrieve_linked_user_logins,
    workflow.retrieve_user_onboarding,
    workflow.choose_user_onboarding_alias,
    workflow.retrieve_claimed_alias,
    workflow.complete_user_onboarding,
    workflow.project_users,
    workflow.cancel_user_onboarding,
    workflow.retrieve_users,
    workflow.retrieve_user_summaries,
    workflow.retrieve_user_profile,
    workflow.retrieve_user_personal_data,
    workflow.request_user_personal_data_erasure,
    workflow.cancel_user_personal_data_erasure,
    workflow.apply_due_user_personal_data_erasures,
    workflow.initiate_business_onboarding,
    workflow.retrieve_business_onboarding,
  ]
}

actor "authenticated_user" {
  auth_required = true
}

actor "system" {
  auth_required = false
}

bounded_context "user" {
  title       = "User"
  description = "User onboarding, authentication and personal data. One module, several aggregates and streams."


  aggregate "user_personal_data" {
    title       = "User Personal Data"
  }

  event "user_personal_data_recorded" {
    title       = "User Personal Data Recorded"
    description = "A person's identifying data was recorded. Written only at user onboarding initiation."
    aggregate   = aggregate.user_personal_data

    field "name" {
      type    = "String"
      example = "Joe"
      pii     = true
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
      pii     = true
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
      pii     = true
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }
  }

  event "user_personal_data_erasure_requested" {
    title       = "User Personal Data Erasure Requested"
    description = "The person asked to be forgotten. The data is still readable until the request is applied, so the request can be canceled during the grace period."
    aggregate   = aggregate.user_personal_data

    field "reason_code" {
      type    = "String"
      example = "user_request"
    }

    field "erase_after_utc" {
      type    = "DateTime"
      example = "2026-11-16T00:00:00.000Z"
    }
  }

  event "user_personal_data_erasure_canceled" {
    title       = "User Personal Data Erasure Canceled"
    description = "The person changed their mind before the erasure was applied. The personal data was never truncated."
    aggregate   = aggregate.user_personal_data
  }

  event "user_personal_data_erasure_applied" {
    title       = "User Personal Data Erasure Applied"
    description = "The erasure was executed: user logins were released and the stream was truncated before UserPersonalDataErasureRequested. This event survives the truncation and is what read models use to anonymize."
    aggregate   = aggregate.user_personal_data
  }


  field_type "linked_user_login" {
    type = "Custom"
  }

  aggregate "user_login_claiming" {
    title       = "User Login Claiming"
  }

  aggregate "user_login_linking" {
    title       = "User Login Linking"
    description = "Stream user_login_linking/{user_id}. Reverse index of the user logins linked to a user, used to release them all when the person is erased."
  }

  event "user_login_claimed" {
    title       = "User Login Claimed"
    description = "A user login was claimed by a user_id. This is the only lookup the token filter needs."
    aggregate   = aggregate.user_login_claiming

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }

  event "user_login_released" {
    title       = "User Login Released"
    description = "The user login no longer belongs to a user_id and can be claimed again."
    aggregate   = aggregate.user_login_claiming

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }
  }

  event "user_login_linked" {
    title       = "User Login Linked"
    description = "A user login was added to a user's index. Used for multi-login accounts."
    aggregate   = aggregate.user_login_linking

    field "user_login_key" {
      type    = "String"
      example = "5f0c2e..."
    }

    field "kind" {
      type    = "String"
      example = "oidc"
    }

    field "issuer" {
      type    = "String"
      example = "https://accounts.google.com"
    }
  }

  event "user_login_unlinked" {
    title       = "User Login Unlinked"
    description = "A user login was removed from a user's index."
    aggregate   = aggregate.user_login_linking

    field "user_login_key" {
      type    = "String"
      example = "5f0c2e..."
    }
  }


  field_type "user" {
    type = "Custom"
  }

  field_type "user_summary" {
    type = "Custom"
  }

  aggregate "user_onboarding" {
    title       = "User Onboarding"
  }

  aggregate "user_alias_claiming" {
    title       = "User Alias Claiming"
    description = "Stream user_alias_claiming/{alias}. An alias can belong to only one user at a time. Released aliases can be claimed again."
  }

  event "user_onboarding_initiated" {
    title       = "User Onboarding Initiated"
    description = "A person started user onboarding. The user_onboarding_id is the stream id; the user_id identifies the person and is recorded once here."
    aggregate   = aggregate.user_onboarding

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }
  }

  event "user_onboarding_alias_chosen" {
    title       = "User Onboarding Alias Chosen"
    description = "An alias was chosen for the user onboarding."
    aggregate   = aggregate.user_onboarding

    field "alias" {
      type    = "String"
      example = "joe-1984"
    }
  }

  event "user_onboarding_completed" {
    title       = "User Onboarding Completed"
    description = "The user onboarding was completed and the terms of service and privacy policy versions were accepted. Consent evidence must survive an erasure, so it lives here and not in user_personal_data."
    aggregate   = aggregate.user_onboarding

    field "terms_of_service_version" {
      type    = "String"
      example = "1.0.0"
    }

    field "privacy_policy_version" {
      type    = "String"
      example = "1.0.0"
    }
  }

  event "user_onboarding_canceled" {
    title       = "User Onboarding Canceled"
    description = "The user onboarding was canceled. Only a reason code is stored; free text is not allowed because it could contain PII."
    aggregate   = aggregate.user_onboarding

    field "reason_code" {
      type    = "String"
      example = "changed_mind"
    }
  }

  event "alias_claimed" {
    title       = "Alias Claimed"
    description = "An alias was claimed by a user."
    aggregate   = aggregate.user_alias_claiming

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }
  }

  event "alias_released" {
    title       = "Alias Released"
    description = "An alias was released by a user and can be claimed again."
    aggregate   = aggregate.user_alias_claiming

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }
  }
}

bounded_context "business" {
  title       = "Business"
  description = "Business onboarding."


  aggregate "business_onboarding" {
    title       = "Business Onboarding"
  }

  event "business_onboarding_initiated" {
    title       = "Business Onboarding Initiated"
    description = "A business onboarding process was initiated for a user."
    aggregate   = aggregate.business_onboarding

    field "legal_name" {
      type    = "String"
      example = "Acme Ltd"
    }

    field "initiated_by" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }
  }


}

