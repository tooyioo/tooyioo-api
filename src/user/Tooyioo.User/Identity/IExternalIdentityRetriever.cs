using Microsoft.AspNetCore.Http;

namespace Tooyioo.User.Identity;

public interface IExternalIdentityRetriever
{
    ExternalIdentity GetExternalIdentityFromContext(HttpContext context);
}