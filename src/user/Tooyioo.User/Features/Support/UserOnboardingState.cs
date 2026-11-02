using Eventuous;
using Tooyioo.User.Contracts;

namespace Tooyioo.User.Features.Support;

public sealed record UserOnboardingState
    : State<UserOnboardingState, UserOnboardingId>
{
    public string? UserId { get; private init; }
    public string? Alias { get; private init; }
    public bool IsInitiated { get; private init; }
    public bool IsComplete { get; private init; }
    public bool IsCanceled { get; private init; }
    public string? CancellationReasonCode { get; private init; }

    public UserOnboardingState()
    {
        On<UserOnboardingDomainEvents.V1.UserOnboardingInitiated>(Initiated);
        On<UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen>(AliasChosen);
        On<UserOnboardingDomainEvents.V1.UserOnboardingCompleted>(Completed);
        On<UserOnboardingDomainEvents.V1.UserOnboardingCanceled>(Canceled);
    }

    private static UserOnboardingState Initiated(
        UserOnboardingState state,
        UserOnboardingDomainEvents.V1.UserOnboardingInitiated domainEvent)
        => state with { IsInitiated = true, UserId = domainEvent.UserId };

    private static UserOnboardingState AliasChosen(
        UserOnboardingState state,
        UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen domainEvent)
        => state with { Alias = domainEvent.Alias };

    private static UserOnboardingState Completed(
        UserOnboardingState state,
        UserOnboardingDomainEvents.V1.UserOnboardingCompleted domainEvent)
        => state with { IsComplete = true };

    private static UserOnboardingState Canceled(
        UserOnboardingState state,
        UserOnboardingDomainEvents.V1.UserOnboardingCanceled domainEvent)
        => state with { IsCanceled = true, CancellationReasonCode = domainEvent.ReasonCode };
}
