using Eventuous;
using Tooyioo.User.Features.CancelUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.CancelUserOnboarding;

public sealed class CancelUserOnboardingAuthorizer(IEventReader eventReader)
    : UserOnboardingRouteAuthorizer<CancelUserOnboardingRoute>(eventReader)
{
    protected override Guid GetUserOnboardingId(CancelUserOnboardingRoute route)
        => route.UserOnboardingId;
}
