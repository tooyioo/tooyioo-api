using Eventuous;
using Eventuous.Projections.MongoDB;
using Eventuous.Subscriptions.Context;
using MongoDB.Driver;
using Tooyioo.User.Contracts;
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.Support;

public sealed class UserProjector
    : MongoProjector<UserDocument>
{
    private const string ErasedValue = "[erased]";

    public UserProjector(
        IMongoDatabase database,
        ITypeMapper typeMapper)
        : base(database, null, typeMapper)
    {
        On<UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded>(
            stream => stream.GetId(), HandleRecorded);
        On<UserOnboardingDomainEvents.V1.UserOnboardingInitiated>(
            stream => stream.GetId(), HandleInitiated);
        On<UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen>(
            stream => stream.GetId(), HandleAliasChosen);
        On<UserOnboardingDomainEvents.V1.UserOnboardingCompleted>(
            stream => stream.GetId(), HandleCompleted);
        On<UserOnboardingDomainEvents.V1.UserOnboardingCanceled>(
            stream => stream.GetId(), HandleCanceled);
        On<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureApplied>(
            stream => stream.GetId(), HandleErasureApplied);
    }

    private static UpdateDefinition<UserDocument> HandleRecorded(
        IMessageConsumeContext<UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded> ctx,
        UpdateDefinitionBuilder<UserDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .SetOnInsert(x => x.CreatedAt, happenedAtUtc)
            .SetOnInsert(x => x.PhoneNumber, string.Empty)
            .SetOnInsert(x => x.Status, UserDocument.StatusInProgress)
            .Set(x => x.Name, evt.Name)
            .Set(x => x.LastName, evt.LastName)
            .Set(x => x.Email, evt.Email)
            .Set(x => x.IsEmailVerified, evt.IsEmailVerified)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<UserDocument> HandleInitiated(
        IMessageConsumeContext<UserOnboardingDomainEvents.V1.UserOnboardingInitiated> ctx,
        UpdateDefinitionBuilder<UserDocument> update)
    {
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .SetOnInsert(x => x.CreatedAt, happenedAtUtc)
            .SetOnInsert(x => x.PhoneNumber, string.Empty)
            .SetOnInsert(x => x.Name, string.Empty)
            .SetOnInsert(x => x.LastName, string.Empty)
            .SetOnInsert(x => x.Email, string.Empty)
            .Set(x => x.Alias, null)
            .Set(x => x.Status, UserDocument.StatusInProgress)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<UserDocument> HandleAliasChosen(
        IMessageConsumeContext<UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen> ctx,
        UpdateDefinitionBuilder<UserDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .Set(x => x.Alias, evt.Alias)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<UserDocument> HandleCompleted(
        IMessageConsumeContext<UserOnboardingDomainEvents.V1.UserOnboardingCompleted> ctx,
        UpdateDefinitionBuilder<UserDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .Set(x => x.Status, UserDocument.StatusCompleted)
            .Set(x => x.TermsOfServiceVersion, evt.TermsOfServiceVersion)
            .Set(x => x.PrivacyPolicyVersion, evt.PrivacyPolicyVersion)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<UserDocument> HandleCanceled(
        IMessageConsumeContext<UserOnboardingDomainEvents.V1.UserOnboardingCanceled> ctx,
        UpdateDefinitionBuilder<UserDocument> update)
    {
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .Set(x => x.Alias, null)
            .Set(x => x.Status, UserDocument.StatusCanceled)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }

    private static UpdateDefinition<UserDocument> HandleErasureApplied(
        IMessageConsumeContext<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureApplied> ctx,
        UpdateDefinitionBuilder<UserDocument> update)
    {
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);

        return update
            .SetOnInsert(x => x.Id, ctx.Stream.GetId())
            .SetOnInsert(x => x.PhoneNumber, string.Empty)
            .SetOnInsert(x => x.CreatedAt, happenedAtUtc)
            .Set(x => x.Name, ErasedValue)
            .Set(x => x.LastName, ErasedValue)
            .Set(x => x.Email, ErasedValue)
            .Set(x => x.IsEmailVerified, false)
            .Set(x => x.Alias, null)
            .Set(x => x.Status, UserDocument.StatusErased)
            .Set(x => x.LastModifiedAt, happenedAtUtc)
            .Set(x => x.Revision, ctx.StreamPosition);
    }
}
