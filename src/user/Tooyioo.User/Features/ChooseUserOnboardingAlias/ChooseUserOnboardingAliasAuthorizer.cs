using Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;
using Tooyioo.User.Features.Support;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.ChooseUserOnboardingAlias;

public sealed class ChooseUserOnboardingAliasAuthorizer
    : UserOnboardingRouteAuthorizer<ChooseUserOnboardingAliasRoute>
{
    protected override Guid GetUserId(ChooseUserOnboardingAliasRoute route)
        => route.UserId;
}
