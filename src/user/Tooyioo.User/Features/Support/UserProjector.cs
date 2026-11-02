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

    private readonly IUserPersonalDataReader _personalDataReader;

    public UserProjector(
        IMongoDatabase database,
        IUserPersonalDataReader personalDataReader,
        ITypeMapper typeMapper)
        : base(database, null, typeMapper)
    {
        _personalDataReader = personalDataReader;

        OnAsync<UserOnboardingDomainEvents.V1.UserOnboardingInitiated>(
            evt => evt.UserId, HandleInitiated);
        On<UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen>(
            (ctx, filter) => filter.Eq(x => x.UserOnboardingId, ctx.Stream.GetId()), HandleAliasChosen);
        On<UserOnboardingDomainEvents.V1.UserOnboardingCompleted>(
            (ctx, filter) => filter.Eq(x => x.UserOnboardingId, ctx.Stream.GetId()), HandleCompleted);
        On<UserOnboardingDomainEvents.V1.UserOnboardingCanceled>(
            (ctx, filter) => filter.Eq(x => x.UserOnboardingId, ctx.Stream.GetId()), HandleCanceled);
        On<UserPersonalDataDomainEvents.V1.UserPersonalDataErasureApplied>(
            stream => stream.GetId(), HandleErasureApplied);
    }

    private async ValueTask<UpdateDefinition<UserDocument>> HandleInitiated(
        IMessageConsumeContext<UserOnboardingDomainEvents.V1.UserOnboardingInitiated> ctx,
        UpdateDefinitionBuilder<UserDocument> update)
    {
        var evt = ctx.Message;
        var happenedAtUtc = DateTime.SpecifyKind(ctx.Created, DateTimeKind.Utc);
        var personalData = await _personalDataReader.Get(evt.UserId, ctx.CancellationToken);

        return update
            .SetOnInsert(x => x.Id, evt.UserId)
            .Set(x => x.UserOnboardingId, ctx.Stream.GetId())
            .Set(x => x.Name, personalData.Name)
            .Set(x => x.LastName, personalData.LastName)
            .Set(x => x.Email, personalData.Email)
            .Set(x => x.IsEmailVerified, personalData.IsEmailVerified)
            .Set(x => x.PhoneNumber, string.Empty)
            .Set(x => x.Alias, null)
            .Set(x => x.Status, UserDocument.StatusInProgress)
            .Set(x => x.CreatedAt, happenedAtUtc)
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
            .SetOnInsert(x => x.UserOnboardingId, string.Empty)
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
