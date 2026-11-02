using Eventuous;

namespace Tooyioo.User.Features.Support;

public sealed record UserLoginLinkingId(string Value)
    : Id(Value)
{
    public static implicit operator UserLoginLinkingId(string value) => new(value);
    public static implicit operator string(UserLoginLinkingId id) => id.Value;
}
