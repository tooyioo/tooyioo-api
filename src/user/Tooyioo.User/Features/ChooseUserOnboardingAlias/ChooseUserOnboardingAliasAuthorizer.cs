using Eventuous;
using Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;
using Tooyioo.User.Features.Support;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.ChooseUserOnboardingAlias;

public sealed class ChooseUserOnboardingAliasAuthorizer(IEventReader eventReader)
    : UserOnboardingRouteAuthorizer<ChooseUserOnboardingAliasRoute>(eventReader)
{
    protected override Guid GetUserOnboardingId(ChooseUserOnboardingAliasRoute route)
        => route.UserOnboardingId;
}
