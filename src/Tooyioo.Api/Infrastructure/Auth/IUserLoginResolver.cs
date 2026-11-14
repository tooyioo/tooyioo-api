using Tooyioo.User;
using Tooyioo.User.Identity;

namespace Tooyioo.Api.Infrastructure.Auth;

internal interface IUserLoginResolver
{
    ValueTask<UserId?> Resolve(ExternalIdentity externalIdentity, CancellationToken cancellationToken);
}
