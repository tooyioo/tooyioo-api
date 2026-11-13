using Tooyioo.Common;

namespace Tooyioo.Api.Infrastructure.Auth;

internal interface IUserLoginResolver
{
    ValueTask<UserId?> Resolve(ExternalIdentity externalIdentity, CancellationToken cancellationToken);
}
