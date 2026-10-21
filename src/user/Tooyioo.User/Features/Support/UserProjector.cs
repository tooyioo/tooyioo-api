using Eventuous.Projections.MongoDB;
using Eventuous.Subscriptions.Context;
using Eventuous;
using MongoDB.Driver;
using Tooyioo.UserOnboarding.Contracts;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UseDeconstruction

namespace Tooyioo.User.Features.Support;

public sealed class UserProjector
    : MongoProjector<UserDocument>
{
    private readonly IUserOnboardingDetailsRetriever _userOnboardingDetailsRetriever;

    public UserProjector(
        IMongoDatabase database,
        IUserOnboardingDetailsRetriever userOnboardingDetailsRetriever, ITypeMapper typeMapper)
        : base(database, null, typeMapper)
    {
        _userOnboardingDetailsRetriever = userOnboardingDetailsRetriever;
        
        OnAsync<UserOnboardingDomainEvents.V1.UserOnboardingCompleted>(evt => evt.UserId, Handle);
    }

    private async ValueTask<UpdateDefinition<UserDocument>> Handle(
        IMessageConsumeContext<UserOnboardingDomainEvents.V1.UserOnboardingCompleted> ctx, 
        UpdateDefinitionBuilder<UserDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);
        var userId = evt.UserId;
        var userOnboardingId = ctx.Stream.GetId();
        var userOnboardingDetails = 
            await _userOnboardingDetailsRetriever.Retrieve(userOnboardingId, ctx.CancellationToken);
        
        if (!userOnboardingDetails.IsSome(out var userOnboardingDetailsValue))
        {
            throw new InvalidOperationException(
                $"Could not find user onboarding details for Id {userOnboardingId} when projecting user with Id {userId}");
        }
        
        return update
            .SetOnInsert(x => x.Id, userId)
            .Set(x => x.TermsAndConditionsVersion, evt.TermsAndConditionsVersion)
            .Set(x => x.Name, userOnboardingDetailsValue.Name)
            .Set(x => x.LastName, userOnboardingDetailsValue.LastName)
            .Set(x => x.Email, userOnboardingDetailsValue.Email)
            .Set(x => x.IsEmailVerified, userOnboardingDetailsValue.IsEmailVerified)
            .Set(x => x.ExternalIdentityId, userOnboardingDetailsValue.ExternalId)
            .Set(x => x.ExternalIdentityProvider, userOnboardingDetailsValue.ExternalProvider)
            .Set(x => x.PhoneNumber, string.Empty)
            .Set(x => x.Alias, userOnboardingDetailsValue.Alias)
            .Set(x => x.UserOnboardingId, userOnboardingId)
            .Set(x => x.CreatedAt, happenedAtUtc)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, 1u);
    }
}
