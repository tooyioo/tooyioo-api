# Chapters group workflows into readable timelines.

chapter "user_onboarding_happy_path" {
  title = "Chapter 1 - User Onboarding"
  workflows = [
    workflow.initiate_user_onboarding,
    workflow.retrieve_claimed_identity,
    workflow.retrieve_user_onboarding,
    workflow.choose_user_onboarding_alias,
    workflow.retrieve_claimed_alias,
    workflow.retrieve_user_onboarding_after_alias_chosen,
    workflow.complete_user_onboarding,
    workflow.retrieve_user_onboarding_after_completion,
    workflow.project_users,
    workflow.retrieve_user_profile,
  ]
}

chapter "user_onboarding_cancellation" {
  title = "Chapter 2 - Alternative: User Onboarding Cancellation"
  workflows = [
    workflow.cancel_user_onboarding,
    workflow.retrieve_user_onboarding_after_cancellation,
  ]
}

chapter "user_directory" {
  title = "Chapter 3 - User Directory"
  workflows = [
    workflow.retrieve_users,
    workflow.retrieve_user_summaries,
  ]
}

# emhcl lists chapters by ID; the z_ prefix keeps Business Onboarding last.
chapter "z_business_onboarding" {
  title = "Chapter 4 - Business Onboarding"
  workflows = [
    workflow.initiate_business_onboarding,
  ]
}
