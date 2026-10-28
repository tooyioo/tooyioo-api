state_view "retrieve_claimed_alias" {
  title  = "Retrieve Claimed Alias"
  status = "done"

  readmodel "claimed_alias" {
    title    = "Claimed Alias"
    question = "Which user onboarding has claimed this alias?"
    from     = [event.onboarding.alias_claimed]

    field "id" {
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
