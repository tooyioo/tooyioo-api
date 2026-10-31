state_view "retrieve_claimed_identity" {
  title  = "Retrieve Claimed Identity"
  description = "This read model is an in-memory lookup, not an HTTP resource. Initiate User Onboarding uses it to determine whether the external identity is already claimed globally and, if so, which onboarding owns it."
  status = "done"

  readmodel "claimed_identity" {
    title       = "Claimed Identity"
    description = "In-memory command-side state rehydrated from the External Identity Claiming stream. It is not an HTTP resource and is not paginated."
    question    = "What is the user onboarding that claimed this external identity?"
    from = [
      event.onboarding.external_identity_claimed
    ]

    field "external_identity_id" {
      type         = "String"
      id_attribute = true
      example      = "abc123"
    }

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }

  scenario "scenario_01_external_identity_is_claimed" {
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
