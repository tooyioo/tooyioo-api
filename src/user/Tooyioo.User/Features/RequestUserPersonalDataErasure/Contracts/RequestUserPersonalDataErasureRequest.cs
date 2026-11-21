using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace Tooyioo.User.Features.RequestUserPersonalDataErasure.Contracts;

/// <summary>
/// Request payload used to request the erasure of the authenticated user's personal data.
/// </summary>
/// <example>{"reasonCode": "user_request"}</example>
[ValidatableType]
public sealed record RequestUserPersonalDataErasureRequest
{
    public const int ReasonCodeMaxLength = 50;

    /// <summary>
    /// A bounded reason code. Free text is not accepted because it could contain personal data.
    /// </summary>
    [Required(ErrorMessage = "reason_code_required")]
    [MaxLength(ReasonCodeMaxLength, ErrorMessage = "reason_code_too_long")]
    public string ReasonCode { get; init; } = string.Empty;
}
