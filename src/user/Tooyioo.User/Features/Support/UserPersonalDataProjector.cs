using Eventuous;
using Eventuous.Projections.MongoDB;
using Eventuous.Subscriptions.Context;
using MongoDB.Driver;
using Tooyioo.User.Contracts;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.Support;

public sealed class UserPersonalDataProjector
    : MongoProjector<UserPersonalDataDocument>
{
    private const string ErasedValue = "[erased]";

    public UserPersonalDataProjector(IMongoDatabase database, ITypeMapper typeMapper)
        : base(database, null, typeMapper)
    {
        On<UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded>(stream => stream.GetId(), HandleRecorded);
        On<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureRequested>(stream => stream.GetId(), HandleRequested);
        On<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureCanceled>(stream => stream.GetId(), HandleCanceled);
        On<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureApplied>(stream => stream.GetId(), HandleApplied);
    }

    private static UpdateDefinition<UserPersonalDataDocument> HandleRecorded(
        IMessageConsumeContext<UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded> ctx,
        UpdateDefinitionBuilder<UserPersonalDataDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .Set(x => x.Name, evt.Name)
            .Set(x => x.LastName, evt.LastName)
            .Set(x => x.Email, evt.Email)
            .Set(x => x.IsEmailVerified, evt.IsEmailVerified)
            .Set(x => x.Status, UserPersonalDataDocument.StatusActive)
            .Set(x => x.IsErasureRequested, false)
            .Set(x => x.IsErased, false)
            .Set(x => x.EraseAfterUtc, null)
            .Set(x => x.CreatedAt, happenedAtUtc)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<UserPersonalDataDocument> HandleRequested(
        IMessageConsumeContext<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureRequested> ctx,
        UpdateDefinitionBuilder<UserPersonalDataDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .SetOnInsert(x => x.Name, string.Empty)
            .SetOnInsert(x => x.LastName, string.Empty)
            .SetOnInsert(x => x.Email, string.Empty)
            .SetOnInsert(x => x.IsEmailVerified, false)
            .SetOnInsert(x => x.CreatedAt, happenedAtUtc)
            .Set(x => x.Status, UserPersonalDataDocument.StatusErasureRequested)
            .Set(x => x.IsErasureRequested, true)
            .Set(x => x.IsErased, false)
            .Set(x => x.EraseAfterUtc, evt.EraseAfterUtc)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<UserPersonalDataDocument> HandleCanceled(
        IMessageConsumeContext<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureCanceled> ctx,
        UpdateDefinitionBuilder<UserPersonalDataDocument> update)
    {
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .SetOnInsert(x => x.Name, string.Empty)
            .SetOnInsert(x => x.LastName, string.Empty)
            .SetOnInsert(x => x.Email, string.Empty)
            .SetOnInsert(x => x.IsEmailVerified, false)
            .SetOnInsert(x => x.CreatedAt, happenedAtUtc)
            .Set(x => x.Status, UserPersonalDataDocument.StatusActive)
            .Set(x => x.IsErasureRequested, false)
            .Set(x => x.IsErased, false)
            .Set(x => x.EraseAfterUtc, null)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<UserPersonalDataDocument> HandleApplied(
        IMessageConsumeContext<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureApplied> ctx,
        UpdateDefinitionBuilder<UserPersonalDataDocument> update)
    {
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .SetOnInsert(x => x.CreatedAt, happenedAtUtc)
            .Set(x => x.Name, ErasedValue)
            .Set(x => x.LastName, ErasedValue)
            .Set(x => x.Email, ErasedValue)
            .Set(x => x.IsEmailVerified, false)
            .Set(x => x.Status, UserPersonalDataDocument.StatusErased)
            .Set(x => x.IsErasureRequested, false)
            .Set(x => x.IsErased, true)
            .Set(x => x.EraseAfterUtc, null)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }
}
