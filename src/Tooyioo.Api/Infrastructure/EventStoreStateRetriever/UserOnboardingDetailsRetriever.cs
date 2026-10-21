using Eventuous;
using Funzo;
using Slicent.EventStore;
using Tooyioo.User.Features.Support;
using Tooyioo.UserOnboarding;

// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.Api.Infrastructure.EventStoreStateRetriever;

public sealed class UserOnboardingDetailsRetriever
    : IUserOnboardingDetailsRetriever
{
    private readonly IEventReader _eventReader;

    public UserOnboardingDetailsRetriever(IEventReader eventReader)
    {
        _eventReader = eventReader;
    }
    
    public async Task<Option<UserOnboardingDetails>> Retrieve(string userOnboardingId, CancellationToken cancellationToken)
    {
        var userOnboarding =
            await _eventReader.LoadStateOrNew<UserOnboardingState, UserOnboardingId>(
                new UserOnboardingId(userOnboardingId),
                cancellationToken);

        if (userOnboarding.Events.Length == 0)
        {
            return Option<UserOnboardingDetails>.None;
        }

        var userOnboardingState = userOnboarding.State;

        return new UserOnboardingDetails
        {
            Id = userOnboardingState.Id,
            Alias = userOnboardingState.Alias ?? string.Empty,
            Name = userOnboardingState.Name,
            LastName = userOnboardingState.LastName,
            Email = userOnboardingState.Email,
            IsEmailVerified = userOnboardingState.IsEmailVerified,
            ExternalId = userOnboardingState.ExternalIdentityId,
            ExternalProvider = userOnboardingState.ExternalIdentityProvider
        };
    }
}