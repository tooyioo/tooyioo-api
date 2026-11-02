using Eventuous;
using Eventuous.Projections.MongoDB;
using Eventuous.Subscriptions.Context;
using MongoDB.Driver;
using Tooyioo.Business.Contracts;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.Business.Features.Support;

public sealed class BusinessOnboardingProjector
    : MongoProjector<BusinessOnboardingDocument>
{
    public BusinessOnboardingProjector(IMongoDatabase database, ITypeMapper typeMapper)
        : base(database, null, typeMapper)
    {
        On<BusinessOnboardingDomainEvents.V1.BusinessOnboardingInitiated>(stream => stream.GetId(), Handle);
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
            .Set(x => x.CreatedAt, happenedAtUtc)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }
}
