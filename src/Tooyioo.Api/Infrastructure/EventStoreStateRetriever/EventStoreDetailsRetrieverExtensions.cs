using Tooyioo.User.Features.Support;

namespace Tooyioo.Api.Infrastructure.EventStoreStateRetriever;

public static class EventStoreDetailsRetrieverExtensions
{
    public static TBuilder AddEventStoreDetailsRetrievers<TBuilder>(this TBuilder builder) where TBuilder
        : IHostApplicationBuilder
    {
        builder.Services.AddTransient<IUserPersonalDataDetailsRetriever, UserPersonalDataDetailsRetriever>();
        builder.Services.AddTransient<IUserPersonalDataReader, UserPersonalDataReader>();
        return builder;
    }
}
