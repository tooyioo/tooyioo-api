# Each chapter presents one alternative onboarding timeline.

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
  ]
}

chapter "user_onboarding_cancellation" {
  title = "Chapter 2 - Alternative: User Onboarding Cancellation"
  workflows = [
    workflow.cancel_user_onboarding,
    workflow.retrieve_user_onboarding_after_cancellation,
  ]
}
