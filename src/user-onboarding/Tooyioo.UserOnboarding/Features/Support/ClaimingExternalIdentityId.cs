// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

using Eventuous;

namespace Tooyioo.UserOnboarding.Features.Support;

public sealed record ClaimingExternalIdentityId(string Value)
    : Id(Value)
{
    public static implicit operator ClaimingExternalIdentityId(string id) => new(id);
    public static implicit operator string(ClaimingExternalIdentityId id) => id.Value;
}