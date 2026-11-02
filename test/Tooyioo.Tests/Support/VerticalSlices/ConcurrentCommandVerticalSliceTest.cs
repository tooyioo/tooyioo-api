using Eventuous;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tooyioo.Tests.Support.Events;

namespace Tooyioo.Tests.Support.VerticalSlices;

public abstract class ConcurrentCommandVerticalSliceTest
    : CommandVerticalSliceTest
{
    protected override Task<VerticalSliceTestHost> CreateHost()
        => VerticalSliceTestHost.Start(services =>
        {
            services.RemoveAll<IEventWriter>();
            services.AddSingleton<IEventWriter>(new ThrowingEventWriter());
        });
}
