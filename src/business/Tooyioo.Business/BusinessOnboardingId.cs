using Eventuous;

namespace Tooyioo.Business;

public sealed record BusinessOnboardingId(string Value)
    : Id(Value)
{
    public static BusinessOnboardingId New() => new(Guid.NewGuid().ToString());
    public static implicit operator BusinessOnboardingId(string value) => new(value);
    public static implicit operator string(BusinessOnboardingId id) => id.Value;
}
