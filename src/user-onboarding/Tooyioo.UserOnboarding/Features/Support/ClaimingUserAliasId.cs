// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

using Eventuous;

namespace Tooyioo.UserOnboarding.Features.Support;

internal sealed record ClaimingUserAliasId(string Value)
    : Id(Value)
{
    public static implicit operator ClaimingUserAliasId(string id) => new(id);
    public static implicit operator string(ClaimingUserAliasId id) => id.Value;
}