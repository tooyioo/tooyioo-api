namespace Tooyioo.User.Identity;

public interface IUserLoginKeyFactory
{
    UserLoginKey ForOidc(string issuer, string subject);
}
