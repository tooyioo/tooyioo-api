using Eventuous;
using Tooyioo.User.Contracts;

namespace Tooyioo.User.Features.Support;

public sealed record UserPersonalDataState
    : State<UserPersonalDataState, UserPersonalDataId>
{
    public bool IsRecorded { get; private init; }
    public string? Name { get; private init; }
    public string? LastName { get; private init; }
    public string? Email { get; private init; }
    public bool IsEmailVerified { get; private init; }
    public bool IsErasureRequested { get; private init; }
    public DateTime? EraseAfterUtc { get; private init; }
    public bool IsErased { get; private init; }

    public UserPersonalDataState()
    {
        On<UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded>(Recorded);
        On<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureRequested>(ErasureRequested);
        On<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureCanceled>(ErasureCanceled);
        On<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureApplied>(ErasureApplied);
    }

    private static UserPersonalDataState Recorded(
        UserPersonalDataState state,
        UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded domainEvent)
        => state with
        {
            IsRecorded = true,
            Name = domainEvent.Name,
            LastName = domainEvent.LastName,
            Email = domainEvent.Email,
            IsEmailVerified = domainEvent.IsEmailVerified
        };

    private static UserPersonalDataState ErasureRequested(
        UserPersonalDataState state,
        UserPersonalDataDomainEvents.V1.UserPersonalDataErasureRequested domainEvent)
        => state with
        {
            IsErasureRequested = true,
            EraseAfterUtc = domainEvent.EraseAfterUtc
        };

    private static UserPersonalDataState ErasureCanceled(
        UserPersonalDataState state,
        UserPersonalDataDomainEvents.V1.UserPersonalDataErasureCanceled domainEvent)
        => state with
        {
            IsErasureRequested = false,
            EraseAfterUtc = null
        };

    private static UserPersonalDataState ErasureApplied(
        UserPersonalDataState state,
        UserPersonalDataDomainEvents.V1.UserPersonalDataErasureApplied domainEvent)
        => state with
        {
            IsErasureRequested = false,
            EraseAfterUtc = null,
            IsErased = true,
            Name = null,
            LastName = null,
            Email = null,
            IsEmailVerified = false
        };
}
