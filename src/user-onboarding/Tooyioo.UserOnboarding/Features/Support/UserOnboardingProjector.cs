using Eventuous;
using Eventuous.Projections.MongoDB;
using Eventuous.Subscriptions.Context;
using MongoDB.Driver;
using Tooyioo.UserOnboarding.Contracts;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.UserOnboarding.Features.Support;

public sealed class UserOnboardingProjector
    : MongoProjector<UserOnboardingDocument>
{
    public UserOnboardingProjector(IMongoDatabase database, ITypeMapper typeMapper)
        : base(database, null, typeMapper)
    {
        On<UserOnboardingDomainEvents.V1.UserOnboardingInitiated>(stream => stream.GetId(), Handle);
        On<UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen>(stream => stream.GetId(), Handle);
        On<UserOnboardingDomainEvents.V1.UserOnboardingCompleted>(stream => stream.GetId(), Handle);
        On<UserOnboardingDomainEvents.V1.UserOnboardingCanceled>(stream => stream.GetId(), Handle);
    }

    private static UpdateDefinition<UserOnboardingDocument> Handle(
        IMessageConsumeContext<UserOnboardingDomainEvents.V1.UserOnboardingInitiated> ctx, 
        UpdateDefinitionBuilder<UserOnboardingDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .Set(x => x.Name, evt.Name)
            .Set(x => x.LastName, evt.LastName)
            .Set(x => x.Email, evt.Email)
            .Set(x => x.IsEmailVerified, evt.IsEmailVerified)
            .Set(x => x.ExternalIdentityId, evt.ExternalIdentityId)
            .Set(x => x.ExternalIdentityProvider, evt.ExternalIdentityProvider)
            .Set(x => x.ExternalIdentityIssuer, evt.ExternalIdentityIssuer)
            .Set(x => x.Status, UserOnboardingDocument.StatusInProgress)
            .Set(x => x.CompletedAt, null)
            .Set(x => x.CanceledAt, null)
            .Set(x => x.CancellationReason, null)
            .Set(x => x.CreatedAt, happenedAtUtc)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }
    
    private static UpdateDefinition<UserOnboardingDocument> Handle(
        IMessageConsumeContext<UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen> ctx, 
        UpdateDefinitionBuilder<UserOnboardingDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .Set(x => x.Alias, evt.Alias)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<UserOnboardingDocument> Handle(
        IMessageConsumeContext<UserOnboardingDomainEvents.V1.UserOnboardingCompleted> ctx,
        UpdateDefinitionBuilder<UserOnboardingDocument> update)
    {
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .Set(x => x.Status, UserOnboardingDocument.StatusCompleted)
            .Set(x => x.CompletedAt, happenedAtUtc)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<UserOnboardingDocument> Handle(
        IMessageConsumeContext<UserOnboardingDomainEvents.V1.UserOnboardingCanceled> ctx,
        UpdateDefinitionBuilder<UserOnboardingDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .Set(x => x.Status, UserOnboardingDocument.StatusCanceled)
            .Set(x => x.CanceledAt, happenedAtUtc)
            .Set(x => x.CancellationReason, evt.Reason)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }
}
