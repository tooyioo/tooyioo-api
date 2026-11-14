using Eventuous;

namespace Tooyioo.User;

public sealed record UserId(string Value)
    : Id(Value)
{
    public static UserId New() => new(Guid.NewGuid().ToString());
    public static implicit operator UserId(string value) => new(value);
    public static implicit operator string(UserId id) => id.Value;
}
