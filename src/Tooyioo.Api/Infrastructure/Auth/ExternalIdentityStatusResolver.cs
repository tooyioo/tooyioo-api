using Eventuous;
using Slicent.EventStore;
using Tooyioo.Common;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.Support;

// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.Api.Infrastructure.Auth;

/// <summary>
/// TODO It needs some in-memory cache decorator not to hit the event store once or twice for each request.
/// For now, it's a good enough solution.
/// </summary>
internal sealed class ExternalIdentityStatusResolver
    : IExternalIdentityStatusResolver
{
    private readonly IEventStore _eventStore;

    public ExternalIdentityStatusResolver(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }
    
    public async ValueTask<ExternalIdentityStatusResult> Resolve(
        ExternalIdentity externalIdentity, 
        CancellationToken cancellationToken)
    {
        var claimingExternalIdentity = 
            await _eventStore.LoadStateOrNew<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
                externalIdentity.Id, 
                cancellationToken);

        if (claimingExternalIdentity.State.UserOnboardingId is { } userOnboardingId)
        {
            var userOnboarding = 
                await _eventStore.LoadStateOrNew<UserOnboardingState, UserOnboardingId>(
                    new UserOnboardingId(userOnboardingId), cancellationToken);

            var userId = userOnboarding.State.UserId;
            if (userId is not null)
            {
                return new ExternalIdentityStatusUserOnboarded(userOnboardingId, userId);
            }
            
            return new ExternalIdentityStatusUserOnboarding(userOnboardingId);
        }

        return new ExternalIdentityStatusUserUnknown();
    }
}
