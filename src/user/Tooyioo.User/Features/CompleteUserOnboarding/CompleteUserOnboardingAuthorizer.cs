using Eventuous;
using Tooyioo.User.Features.CompleteUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.CompleteUserOnboarding;

public sealed class CompleteUserOnboardingAuthorizer(IEventReader eventReader)
    : UserOnboardingRouteAuthorizer<CompleteUserOnboardingRoute>(eventReader)
{
    protected override Guid GetUserOnboardingId(CompleteUserOnboardingRoute route)
        => route.UserOnboardingId;
}
