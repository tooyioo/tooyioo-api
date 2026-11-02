using Eventuous;
using Tooyioo.User.Features.RetrieveUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUserOnboarding;

public sealed class RetrieveUserOnboardingAuthorizer(IEventReader eventReader)
    : UserOnboardingRouteAuthorizer<RetrieveUserOnboardingRoute>(eventReader)
{
    protected override Guid GetUserOnboardingId(RetrieveUserOnboardingRoute route)
        => route.UserOnboardingId;
}
