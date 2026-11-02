using Eventuous;
using Tooyioo.User.Contracts;

namespace Tooyioo.User.Features.Support;

public sealed record LinkedUserLogin(
    string UserLoginKey,
    string Kind,
    string Issuer);

public sealed record UserLoginLinkingState
    : State<UserLoginLinkingState, UserLoginLinkingId>
{
    public IReadOnlyList<LinkedUserLogin> Logins { get; private init; } = [];

    public UserLoginLinkingState()
    {
        On<UserLoginLinkingDomainEvents.V1.UserLoginLinked>(Linked);
        On<UserLoginLinkingDomainEvents.V1.UserLoginUnlinked>(Unlinked);
    }

    private static UserLoginLinkingState Linked(
        UserLoginLinkingState state,
        UserLoginLinkingDomainEvents.V1.UserLoginLinked domainEvent)
        => state with
        {
            Logins =
            [
                .. state.Logins.Where(x => x.UserLoginKey != domainEvent.UserLoginKey),
                new LinkedUserLogin(
                    domainEvent.UserLoginKey,
                    domainEvent.Kind,
                    domainEvent.Issuer)
            ]
        };

    private static UserLoginLinkingState Unlinked(
        UserLoginLinkingState state,
        UserLoginLinkingDomainEvents.V1.UserLoginUnlinked domainEvent)
        => state with
        {
            Logins = state.Logins
                .Where(x => x.UserLoginKey != domainEvent.UserLoginKey)
                .ToArray()
        };
}
