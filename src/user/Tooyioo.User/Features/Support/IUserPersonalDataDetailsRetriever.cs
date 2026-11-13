namespace Tooyioo.User.Features.Support;

public interface IUserPersonalDataDetailsRetriever
{
    Task<UserPersonalDataDetails?> Retrieve(string userId, CancellationToken cancellationToken);
}

public sealed record UserPersonalDataDetails(
    string Name,
    string LastName,
    string Email,
    bool IsEmailVerified);
