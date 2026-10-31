using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.BusinessOnboarding.Features.InitiateBusinessOnboarding.Contracts;

/// <summary>
/// Request payload used to initiate business onboarding
/// </summary>
/// <example>{"legalName": "Acme Ltd"}</example>
#pragma warning disable ASP0029
[ValidatableType]
#pragma warning restore ASP0029
public sealed record InitiateBusinessOnboardingRequest
{
    /// <summary>
    /// The legal name of the business being onboarded
    /// </summary>
    [Required(ErrorMessage = "legal_name_required")]
    public string LegalName { get; init; } = string.Empty;
}
