using Eventuous;
using Slicent.EventStore;
using Tooyioo.User.Contracts;

namespace Tooyioo.User.Features.Support;

[StreamName("user_login_linking")]
public sealed record UserLoginLinkingState
    : State<UserLoginLinkingState>, IStreamState<UserId>
{
    public IReadOnlyList<string> Logins { get; private init; } = [];

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
                .. state.Logins.Where(x => x != domainEvent.UserLoginKey),
                domainEvent.UserLoginKey
            ]
        };

    private static UserLoginLinkingState Unlinked(
        UserLoginLinkingState state,
        UserLoginLinkingDomainEvents.V1.UserLoginUnlinked domainEvent)
        => state with
        {
            Logins = state.Logins
                .Where(x => x != domainEvent.UserLoginKey)
                .ToArray()
        };
}
