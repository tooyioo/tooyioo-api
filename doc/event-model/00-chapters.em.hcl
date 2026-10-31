# Chapters group workflows into readable timelines.
# emhcl orders chapters by ID. Zero-padded prefixes (chapter_01 ... chapter_99)
# keep the picker ordered past 10. A leading digit (01_...) is an invalid HCL label.

chapter "chapter_01_user_onboarding" {
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

chapter "chapter_02_user_onboarding_cancellation" {
  title = "Chapter 2 - Alternative: User Onboarding Cancellation"
  workflows = [
    workflow.cancel_user_onboarding,
    workflow.retrieve_user_onboarding_after_cancellation,
  ]
}

chapter "chapter_03_user_directory" {
  title = "Chapter 3 - User Directory"
  workflows = [
    workflow.retrieve_users,
    workflow.retrieve_user_summaries,
  ]
}

chapter "chapter_04_business_onboarding" {
  title = "Chapter 4 - Business Onboarding"
  workflows = [
    workflow.initiate_business_onboarding,
    workflow.retrieve_business_onboarding,
  ]
}
