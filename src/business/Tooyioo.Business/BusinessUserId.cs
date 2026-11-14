using Tooyioo.Common;

namespace Tooyioo.Business;

public sealed record BusinessUserId(string Value)
    : Identifier(Value)
{
    public static implicit operator BusinessUserId(string value) => new(value);
}
