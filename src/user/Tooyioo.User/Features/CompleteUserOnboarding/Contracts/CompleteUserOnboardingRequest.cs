using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace Tooyioo.User.Features.CompleteUserOnboarding.Contracts;

/// <summary>
/// Request payload used to complete user onboarding
/// </summary>
/// <remarks>
/// It's idempotent. If the onboarding is already completed, it will be returned as-is
/// </remarks>
/// <example>{"termsOfServiceVersion": "1.0.0", "privacyPolicyVersion": "1.0.0"}</example>
#pragma warning disable ASP0029
[ValidatableType]
#pragma warning restore ASP0029
public sealed record CompleteUserOnboardingRequest
{
    public const int VersionMaxLength = 50;

    /// <summary>
    /// The version of the terms of service
    /// </summary>
    [Required(ErrorMessage = "terms_of_service_version_required")]
    [MaxLength(VersionMaxLength, ErrorMessage = "terms_of_service_version_too_long")]
    public string TermsOfServiceVersion { get; init; } = string.Empty;

    /// <summary>
    /// The version of the privacy policy
    /// </summary>
    [Required(ErrorMessage = "privacy_policy_version_required")]
    [MaxLength(VersionMaxLength, ErrorMessage = "privacy_policy_version_too_long")]
    public string PrivacyPolicyVersion { get; init; } = string.Empty;
}