using Funzo;
using Tooyioo.Common;

namespace Tooyioo.Api.Infrastructure.Auth;

internal interface IUserLoginResolver
{
    ValueTask<Option<UserId>> Resolve(ExternalIdentity externalIdentity, CancellationToken cancellationToken);
}
