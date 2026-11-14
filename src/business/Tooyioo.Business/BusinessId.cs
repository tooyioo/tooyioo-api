using Eventuous;

namespace Tooyioo.Business;

public sealed record BusinessId(string Value)
    : Id(Value)
{
    public static BusinessId New() => new(Guid.NewGuid().ToString());
    public static implicit operator BusinessId(string value) => new(value);
    public static implicit operator string(BusinessId id) => id.Value;
}
