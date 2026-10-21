using Eventuous;
using Tooyioo.UserOnboarding.Contracts;

namespace Tooyioo.UserOnboarding;

public record UserOnboardingState
    : State<UserOnboardingState, UserOnboardingId>
{
    public string Name { get; private init; } = null!;
    public string LastName { get; private init; } = null!;
    public string Email { get; private init; } = null!;
    public bool IsEmailVerified { get; private init; }
    public string ExternalIdentityId { get; private init; } = null!;
    public string ExternalIdentityProvider { get; private init; } = null!;
    public string ExternalIdentityIssuer { get; private init; } = null!;
    public string? Alias { get; private init; }
    public bool IsComplete { get; private init; }
    public string? UserId { get; private init; }
    public bool IsCanceled { get; private init; }
    public string? CancellationReason { get; private init; }
    
    public UserOnboardingState()
    {
        On<UserOnboardingDomainEvents.V1.UserOnboardingInitiated>(Initiated);
        On<UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen>(UserOnboardingAliasChosen);
        On<UserOnboardingDomainEvents.V1.UserOnboardingCompleted>(UserOnboardingCompleted);
        On<UserOnboardingDomainEvents.V1.UserOnboardingCanceled>(UserOnboardingCanceled);
    }

    private static UserOnboardingState Initiated(
        UserOnboardingState state, 
        UserOnboardingDomainEvents.V1.UserOnboardingInitiated domainEvent)
    {
        return state with
        {
            Name = domainEvent.Name,
            LastName = domainEvent.LastName,
            Email = domainEvent.Email,
            IsEmailVerified = domainEvent.IsEmailVerified,
            ExternalIdentityId = domainEvent.ExternalIdentityId,
            ExternalIdentityProvider = domainEvent.ExternalIdentityProvider,
            ExternalIdentityIssuer = domainEvent.ExternalIdentityIssuer
        };
    }
    
    private static UserOnboardingState UserOnboardingAliasChosen(
        UserOnboardingState state, 
        UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen domainEvent)
    {
        return state with
        {
            Alias = domainEvent.Alias
        };
    }

    private static UserOnboardingState UserOnboardingCompleted(
        UserOnboardingState state,
        UserOnboardingDomainEvents.V1.UserOnboardingCompleted domainEvent)
    {
        return state with
        {
            IsComplete = true,
            UserId = domainEvent.UserId
        };
    }

    private static UserOnboardingState UserOnboardingCanceled(
        UserOnboardingState state,
        UserOnboardingDomainEvents.V1.UserOnboardingCanceled domainEvent)
    {
        return state with
        {
            IsCanceled = true,
            CancellationReason = domainEvent.Reason
        };
    }
}
