state_view "retrieve_claimed_alias" {
  title       = "Retrieve Claimed Alias"
  description = "This read model is an in-memory lookup, not an HTTP resource. Choose User Onboarding Alias uses it to determine whether an alias is already claimed globally and, if so, which onboarding owns it."
  status      = "done"

  readmodel "claimed_alias" {
    title    = "Claimed Alias"
    description = "In-memory command-side state rehydrated from the Alias Claiming stream. It is not an HTTP resource and is not paginated."
    question = "Which user onboarding has claimed this alias?"
    from     = [event.onboarding.alias_claimed]

    field "alias" {
      type         = "String"
      id_attribute = true
      example      = "joe-1984"
    }

    field "user_onboarding_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000001"
    }
  }

  scenario "alias_claim_is_projected" {
    title = "Claimed alias view"

    given {
      title = "The alias is claimed"
      event = event.onboarding.alias_claimed
    }

    then {
      title     = "The alias resolves to its user onboarding"
      readmodel = readmodel.claimed_alias
    }

    comment {
      description = "The alias is the Alias Claiming stream ID; Alias Claimed supplies the associated user onboarding ID."
    }
  }
}
