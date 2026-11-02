using Eventuous;
using Funzo;
using Slicent.EventStore;
using Tooyioo.User.Features.Support;
using Tooyioo.User.Contracts;
// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.Api.Infrastructure.EventStoreStateRetriever;

public sealed class UserPersonalDataDetailsRetriever
    : IUserPersonalDataDetailsRetriever
{
    private readonly IEventReader _eventReader;

    public UserPersonalDataDetailsRetriever(IEventReader eventReader)
    {
        _eventReader = eventReader;
    }

    public async Task<Option<UserPersonalDataDetails>> Retrieve(
        string userId,
        CancellationToken cancellationToken)
    {
        var personalData =
            await _eventReader.LoadStateOrNew<UserPersonalDataState, UserPersonalDataId>(
                new UserPersonalDataId(userId),
                cancellationToken);

        var state = personalData.State;

        if (personalData.Events.Length == 0
            || state.IsErased
            || !state.IsRecorded
            || state.Name is null
            || state.LastName is null
            || state.Email is null)
        {
            return Option<UserPersonalDataDetails>.None;
        }

        return new UserPersonalDataDetails(state.Name, state.LastName, state.Email, state.IsEmailVerified);
    }
}
