using Eventuous;

namespace Tooyioo.User.Features.Support;

public sealed record UserLoginClaimingId(string Value)
    : Id(Value)
{
    public static implicit operator UserLoginClaimingId(string value) => new(value);
    public static implicit operator string(UserLoginClaimingId id) => id.Value;
}
