using Eventuous;

namespace Tooyioo.User;

public sealed record UserOnboardingId(string Value)
    : Id(Value)
{
    public static UserOnboardingId New() => new(Guid.NewGuid().ToString());
    public static implicit operator UserOnboardingId(string value) => new(value);
    public static implicit operator string(UserOnboardingId id) => id.Value;
}
