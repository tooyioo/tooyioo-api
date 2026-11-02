// ReSharper disable ClassNeverInstantiated.Global
namespace Tooyioo.User.Features.InitiateUserOnboarding.Contracts;

/// <summary>
/// Request payload used to initiate user onboarding
/// </summary>
/// <remarks>
/// It's idempotent. If the user onboarding already happened, it will be returned as-is
/// </remarks>
public sealed record InitiateUserOnboardingRequest;