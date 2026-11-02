namespace Tooyioo.Common;

public interface IUserLoginKeyFactory
{
    UserLoginKey ForOidc(string issuer, string subject);
}
