using Eventuous;
using Funzo;
using Slicent.EventStore;
using Tooyioo.Common;
// ReSharper disable ConvertToPrimaryConstructor

using Tooyioo.User.Features.Support;

namespace Tooyioo.Api.Infrastructure.Auth;

internal sealed class UserLoginResolver
    : IUserLoginResolver
{
    private readonly IEventStore _eventStore;
    private readonly IUserLoginKeyFactory _keyFactory;

    public UserLoginResolver(
        IEventStore eventStore,
        IUserLoginKeyFactory keyFactory)
    {
        _eventStore = eventStore;
        _keyFactory = keyFactory;
    }

    public async ValueTask<Option<UserId>> Resolve(ExternalIdentity externalIdentity, CancellationToken cancellationToken)
    {
        var userLoginKey = _keyFactory.ForOidc(externalIdentity.Issuer, externalIdentity.Id);

        var claiming =
            await _eventStore.LoadStateOrNew<UserLoginClaimingState, UserLoginClaimingId>(
                new UserLoginClaimingId(userLoginKey.Key), cancellationToken);

        return claiming.State.UserId is { } userId
            ? Option.Some(new UserId(userId))
            : Option<UserId>.None;
    }
}
