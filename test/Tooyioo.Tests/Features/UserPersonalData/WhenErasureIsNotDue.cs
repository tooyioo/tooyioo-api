using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.ApplyUserPersonalDataErasure;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.UserPersonalData;

[Category(TestCategories.Conflict)]
public sealed class WhenErasureIsNotDue
    : CommandVerticalSliceTest
{
    private UserOnboardingId _userId = null!;
    private ApplyUserPersonalDataErasureCommandResult _result;

    protected override async Task Given()
    {
        _userId = new UserOnboardingId(1.ToGuid().ToString());

        await Host.Given<UserPersonalDataState, UserPersonalDataId>(
            new UserPersonalDataId(_userId.Value),
            DomainEvent.UserPersonalDataRecorded().Build(),
            DomainEvent.UserPersonalDataErasureRequested()
                .WithEraseAfterUtc(DateTime.UtcNow.AddHours(1))
                .Build());
    }

    protected override async Task When()
        => _result = await Host.SendCommand(
            new ApplyUserPersonalDataErasureCommand(new UserPersonalDataId(_userId.Value)));

    [Test]
    public async Task Then_the_erasure_is_not_applied()
    {
        var isNotDue = _result is ApplyUserPersonalDataErasureNotDueError;

        await Assert.That(isNotDue).IsTrue();
    }
}
