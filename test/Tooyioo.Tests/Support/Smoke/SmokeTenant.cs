namespace Tooyioo.Tests.Support.Smoke;

/// <summary>
/// Unique data namespace for a smoke scenario. Two scenarios can run against the same
/// containers as long as they only share the host, not subjects, aliases or emails.
/// </summary>
public sealed record SmokeTenant(string Suffix)
{
    public static SmokeTenant New()
        => new(Guid.NewGuid().ToString("N")[..10]);

    public string Subject(string role)
        => $"smoke-{role}-{Suffix}";

    public string Alias(string role)
        => $"smoke-{role}-{Suffix}";

    public string Email(string role)
        => $"{Subject(role)}@example.com";

    public string Name(string kind)
        => $"smoke-{kind}-{Suffix}";
}
