using Eventuous;

namespace Tooyioo.User.Features.Support;

public sealed record UserPersonalDataId(string Value)
    : Id(Value)
{
    public static implicit operator UserPersonalDataId(string value) => new(value);
    public static implicit operator string(UserPersonalDataId id) => id.Value;
}
