using Eventuous.Projections.MongoDB.Tools;
using MongoDB.Driver;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Api.Infrastructure.Mongo;

internal sealed class MongoReadModelIndexInitializer(IMongoDatabase database)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var collection = database.GetDocumentCollection<UserDocument>();

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(x => x.UserOnboardingId),
                new CreateIndexOptions<UserDocument>
                {
                    Unique = true,
                    Name = "ux_user_document_user_onboarding_id",
                    PartialFilterExpression =
                        Builders<UserDocument>.Filter.Gt(x => x.UserOnboardingId, string.Empty)
                }),
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
