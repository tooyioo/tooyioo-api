using Eventuous;
using Eventuous.Projections.MongoDB;
using Eventuous.Subscriptions.Context;
using MongoDB.Driver;
using Tooyioo.BusinessOnboarding.Contracts;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.BusinessOnboarding.Features.Support;

public sealed class BusinessOnboardingProjector
    : MongoProjector<BusinessOnboardingDocument>
{
    public BusinessOnboardingProjector(IMongoDatabase database, ITypeMapper typeMapper)
        : base(database, null, typeMapper)
    {
        On<BusinessOnboardingDomainEvents.V1.BusinessOnboardingInitiated>(stream => stream.GetId(), Handle);
        On<BusinessOnboardingDomainEvents.V1.BusinessOnboardingCompleted>(stream => stream.GetId(), Handle);
        On<BusinessOnboardingDomainEvents.V1.BusinessOnboardingCanceled>(stream => stream.GetId(), Handle);
    }

    private static UpdateDefinition<BusinessOnboardingDocument> Handle(
        IMessageConsumeContext<BusinessOnboardingDomainEvents.V1.BusinessOnboardingInitiated> ctx,
        UpdateDefinitionBuilder<BusinessOnboardingDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .Set(x => x.LegalName, evt.LegalName)
            .Set(x => x.InitiatedBy, evt.InitiatedBy)
            .Set(x => x.Status, BusinessOnboardingDocument.StatusInProgress)
            .Set(x => x.CompletedAt, null)
            .Set(x => x.CanceledAt, null)
            .Set(x => x.CancellationReason, null)
            .Set(x => x.CreatedAt, happenedAtUtc)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<BusinessOnboardingDocument> Handle(
        IMessageConsumeContext<BusinessOnboardingDomainEvents.V1.BusinessOnboardingCompleted> ctx,
        UpdateDefinitionBuilder<BusinessOnboardingDocument> update)
    {
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .Set(x => x.Status, BusinessOnboardingDocument.StatusCompleted)
            .Set(x => x.CompletedAt, happenedAtUtc)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<BusinessOnboardingDocument> Handle(
        IMessageConsumeContext<BusinessOnboardingDomainEvents.V1.BusinessOnboardingCanceled> ctx,
        UpdateDefinitionBuilder<BusinessOnboardingDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .Set(x => x.Status, BusinessOnboardingDocument.StatusCanceled)
            .Set(x => x.CanceledAt, happenedAtUtc)
            .Set(x => x.CancellationReason, evt.Reason)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }
}
