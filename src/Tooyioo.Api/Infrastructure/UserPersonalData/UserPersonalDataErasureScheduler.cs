using Eventuous.Projections.MongoDB.Tools;
using MongoDB.Driver;
using Slicent.Application.Commands;
using Tooyioo.User.Features.ApplyUserPersonalDataErasure;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Api.Infrastructure.UserPersonalData;

public sealed class UserPersonalDataErasureScheduler
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserPersonalDataErasureScheduler> _logger;

    public UserPersonalDataErasureScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<UserPersonalDataErasureScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ApplyDueErasures(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to process due personal data erasures");
            }
        }
    }

    private async Task ApplyDueErasures(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var collection = database.GetDocumentCollection<UserPersonalDataDocument>();
        var now = DateTime.UtcNow;

        var due = await collection
            .Find(x => x.IsErasureRequested && !x.IsErased && x.EraseAfterUtc <= now)
            .Limit(100)
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return;
        }

        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        foreach (var document in due)
        {
            try
            {
                await dispatcher.Send(
                    new ApplyUserPersonalDataErasureCommand(new UserPersonalDataId(document.Id)),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to apply personal data erasure for user {UserId}", document.Id);
            }
        }
    }
}
