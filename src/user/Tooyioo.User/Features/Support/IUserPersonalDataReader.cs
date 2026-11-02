namespace Tooyioo.User.Features.Support;

public interface IUserPersonalDataReader
{
    Task<UserPersonalDataDetails> Get(string userId, CancellationToken cancellationToken);
}
