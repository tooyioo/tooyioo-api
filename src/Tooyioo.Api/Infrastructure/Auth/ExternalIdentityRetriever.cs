using Tooyioo.User.Identity;

namespace Tooyioo.Api.Infrastructure.Auth;

public sealed class ExternalIdentityRetriever
    : IExternalIdentityRetriever
{
    public ExternalIdentity GetExternalIdentityFromContext(HttpContext context)
        => ExternalIdentityClaimsParser.Parser(context.User);
}