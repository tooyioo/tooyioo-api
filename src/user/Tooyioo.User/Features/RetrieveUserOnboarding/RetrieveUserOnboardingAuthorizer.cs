using Tooyioo.User.Features.RetrieveUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUserOnboarding;

public sealed class RetrieveUserOnboardingAuthorizer
    : UserOnboardingRouteAuthorizer<RetrieveUserOnboardingRoute>
{
    protected override Guid GetUserId(RetrieveUserOnboardingRoute route)
        => route.UserId;
}
