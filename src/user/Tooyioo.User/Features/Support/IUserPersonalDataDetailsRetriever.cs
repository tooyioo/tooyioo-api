using Funzo;

namespace Tooyioo.User.Features.Support;

public interface IUserPersonalDataDetailsRetriever
{
    Task<Option<UserPersonalDataDetails>> Retrieve(string userId, CancellationToken cancellationToken);
}

public sealed record UserPersonalDataDetails(
    string Name,
    string LastName,
    string Email,
    bool IsEmailVerified);
