using Tooyioo.User.Features.CompleteUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.CompleteUserOnboarding;

public sealed class CompleteUserOnboardingAuthorizer
    : UserOnboardingRouteAuthorizer<CompleteUserOnboardingRoute>
{
    protected override Guid GetUserId(CompleteUserOnboardingRoute route)
        => route.UserId;
}
