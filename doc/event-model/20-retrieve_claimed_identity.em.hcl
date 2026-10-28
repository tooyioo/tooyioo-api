state_view "retrieve_claimed_identity" {
  title  = "Retrieve Claimed Identity"
  status = "done"

  readmodel "claimed_identity" {
    title       = "Claimed Identity"
    description = "Claimed Identity and associated user onboarding Id."
    question    = "What is the user onboarding that claimed this external identity?"
    from = [
      event.onboarding.external_identity_claimed
    ]

    field "id" {
      type         = "String"
      id_attribute = true
      example      = "abc123"
    }

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }

  scenario "external_identity_is_claimed" {
    title = "External identity claimed view"

    given {
      title = "The external identity is claimed"
      event = event.onboarding.external_identity_claimed
    }

    then {
      title     = "The user onboarding Id is projected into the external identity view"
      readmodel = readmodel.claimed_identity
    }

    comment {
      description = "The view exposes the association between an external identity and a user onboarding Id"
    }
  }
}
