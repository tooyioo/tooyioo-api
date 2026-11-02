using Eventuous;

namespace Tooyioo.User.Features.Support;

public sealed record UserAliasClaimingId(string Value)
    : Id(Value)
{
    public static implicit operator UserAliasClaimingId(string value) => new(value);
    public static implicit operator string(UserAliasClaimingId id) => id.Value;
}
