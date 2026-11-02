namespace Tooyioo.User.Features.Support;

public sealed class UserPersonalDataErasureOptions
{
    public const string SectionName = "UserPersonalData:Erasure";

    public TimeSpan GracePeriod { get; init; } = TimeSpan.FromDays(14);
}
