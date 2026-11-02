namespace Tooyioo.Common;

public sealed record UserId(string Value)
{
    public static UserId New() => new(Guid.NewGuid().ToString());
    public static implicit operator UserId(string value) => new(value);
    public static implicit operator string(UserId id) => id.Value;
}
