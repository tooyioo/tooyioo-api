using Tooyioo.User.Features.CancelUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.CancelUserOnboarding;

public sealed class CancelUserOnboardingAuthorizer
    : UserOnboardingRouteAuthorizer<CancelUserOnboardingRoute>
{
    protected override Guid GetUserId(CancelUserOnboardingRoute route)
        => route.UserId;
}
