hotspot "id_token_claim_mapping" {
  question = "Personal details and external identity details come from the id_token emitted by the chosen external_identity_provider. Which claims are used for each provider?"
  status   = "open"
  on       = command.initiate_user_onboarding.initiate_user_onboarding
}
