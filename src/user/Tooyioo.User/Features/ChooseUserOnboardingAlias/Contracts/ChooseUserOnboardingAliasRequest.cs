using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;

/// <summary>
/// Request payload used to complete the profile
/// </summary>
/// <example>{"alias": "joe-bloggs-spain_123"}</example>
[ValidatableType]
public sealed record ChooseUserOnboardingAliasRequest
{
    /// <summary>
    /// The user onboarding alias
    /// </summary>
    /// <remarks>
    /// It must be globally unique and contain lowercase letters, numbers, dashes, and underscores only, between 3 and 30 characters in length
    /// </remarks>
    [Required(ErrorMessage = "alias_required")]
    [MinLength(3, ErrorMessage = "alias_too_short")]
    [MaxLength(30, ErrorMessage = "alias_too_long")]
    [RegularExpression("^[a-z0-9_-]+$", ErrorMessage = "alias_invalid_format")]
    public string Alias { get; init; } = null!;
}