using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.ApplyUserPersonalDataErasure;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.UserPersonalData;

[Category(TestCategories.Conflict)]
public sealed class WhenErasureWasNeverRequested
    : CommandVerticalSliceTest
{
    private UserOnboardingId _userId = null!;
    private ApplyUserPersonalDataErasureCommandResult _result = null!;

    protected override async Task Given()
    {
        _userId = new UserOnboardingId(1.ToGuid().ToString());

        await Host.Given<UserPersonalDataState, UserPersonalDataId>(
            new UserPersonalDataId(_userId.Value),
            DomainEvent.UserPersonalDataRecorded().Build());
    }

    protected override async Task When()
        => _result = await Host.SendCommand(
            new ApplyUserPersonalDataErasureCommand(new UserPersonalDataId(_userId.Value)));

    [Test]
    public async Task Then_the_erasure_is_not_applied()
    {
        var isNotRequested = _result.Match(
            _ => false,
            error => error.Match(
                _ => false,
                _ => true,
                _ => false,
                _ => false));

        await Assert.That(isNotRequested).IsTrue();
    }
}
