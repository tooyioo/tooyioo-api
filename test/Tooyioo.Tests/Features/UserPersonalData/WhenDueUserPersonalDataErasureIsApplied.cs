using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Contracts;
using Tooyioo.User.Features.ApplyUserPersonalDataErasure;
using Tooyioo.User.Features.Support;
using Tooyioo.User;

namespace Tooyioo.Tests.Features.UserPersonalData;

[Category(TestCategories.HappyPath)]
// HCL scenario: apply_due_user_personal_data_erasures/scenario_01_due_erasure_is_applied
public sealed class WhenDueUserPersonalDataErasureIsApplied
    : CommandVerticalSliceTest
{
    private UserId _userId = null!;
    private string _subject = null!;

    protected override async Task Given()
    {
        _userId = new UserId(1.ToGuid().ToString());
        _subject = "google-sub-123";

        await Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(_subject),
            DomainEvent.UserLoginClaimed().WithUserId(_userId.Value).Build());

        await Host.GivenLinkedLogin(_userId, _subject);

        await Host.Given<UserPersonalDataState, UserId>(
            new UserId(_userId.Value),
            DomainEvent.UserPersonalDataRecorded().Build(),
            DomainEvent.UserPersonalDataErasureRequested()
                .WithEraseAfterUtc(DateTime.UtcNow.AddMinutes(-1))
                .Build());
    }

    protected override async Task When()
        => _ = await Host.SendCommand(
            new ApplyUserPersonalDataErasureCommand(new UserId(_userId.Value)));

    [Test]
    public async Task Then_the_erasure_is_applied()
        => await Host.Events
            .Stream<UserPersonalDataState, UserId>(new UserId(_userId.Value))
            .ShouldContain<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureApplied>();

    [Test]
    public async Task Then_the_user_login_is_released()
        => await Host.Events
            .Stream<UserLoginClaimingState, UserLoginClaimingId>(
                new UserLoginClaimingId(_subject))
            .ShouldContain<UserLoginClaimingDomainEvents.V1.UserLoginReleased>();

    [Test]
    public async Task Then_the_user_login_is_unlinked()
        => await Host.Events
            .Stream<UserLoginLinkingState, UserId>(
                new UserId(_userId.Value))
            .ShouldContain<UserLoginLinkingDomainEvents.V1.UserLoginUnlinked>();
}
