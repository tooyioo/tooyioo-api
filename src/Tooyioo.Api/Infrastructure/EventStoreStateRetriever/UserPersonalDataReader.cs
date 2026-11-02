using Eventuous;
using Slicent.EventStore;
using Tooyioo.User.Features.Support;
using Tooyioo.User.Contracts;

namespace Tooyioo.Api.Infrastructure.EventStoreStateRetriever;

public sealed class UserPersonalDataReader
    : IUserPersonalDataReader
{
    private const string ErasedValue = "[erased]";

    private readonly IEventReader _eventReader;

    public UserPersonalDataReader(IEventReader eventReader)
    {
        _eventReader = eventReader;
    }

    public async Task<UserPersonalDataDetails> Get(string userId, CancellationToken cancellationToken)
    {
        var personalData =
            await _eventReader.LoadStateOrNew<UserPersonalDataState, UserPersonalDataId>(
                new UserPersonalDataId(userId),
                cancellationToken);

        var state = personalData.State;

        if (personalData.Events.Length == 0 || state.IsErased || !state.IsRecorded)
        {
            return new UserPersonalDataDetails(ErasedValue, ErasedValue, ErasedValue, false);
        }

        return new UserPersonalDataDetails(
            state.Name ?? ErasedValue,
            state.LastName ?? ErasedValue,
            state.Email ?? ErasedValue,
            state.IsEmailVerified);
    }
}
