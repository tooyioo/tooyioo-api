using Eventuous;

namespace Tooyioo.BusinessOnboarding;

public sealed record BusinessOnboardingId(string Value)
    : Id(Value)
{
    public static BusinessOnboardingId New() => new(Guid.NewGuid().ToString());
    public static implicit operator BusinessOnboardingId(string id) => new(id);
    public static implicit operator string(BusinessOnboardingId id) => id.Value;
}
