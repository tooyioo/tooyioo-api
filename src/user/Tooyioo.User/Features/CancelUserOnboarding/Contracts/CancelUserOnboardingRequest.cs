using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace Tooyioo.User.Features.CancelUserOnboarding.Contracts;

/// <summary>
/// Request payload used to cancel user onboarding
/// </summary>
/// <remarks>
/// The handler is defensively idempotent for an already canceled stream, but the public endpoint cannot be retried after the credential is released.
/// </remarks>
/// <example>{"reasonCode": "changed_mind"}</example>
#pragma warning disable ASP0029
[ValidatableType]
#pragma warning restore ASP0029
public sealed record CancelUserOnboardingRequest
{
    public const int ReasonCodeMaxLength = 50;

    /// <summary>
    /// The reason code for the cancellation
    /// </summary>
    [Required(ErrorMessage = "reason_code_required")]
    [MaxLength(ReasonCodeMaxLength, ErrorMessage = "reason_code_too_long")]
    public string ReasonCode { get; init; } = string.Empty;
}