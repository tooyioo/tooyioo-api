using System.Runtime.CompilerServices;

namespace Tooyioo.Tests.Support;

internal static class TestEnvironment
{
    /// <summary>
    /// Each WebApplicationFactory host creates a physical file watcher for the API
    /// appsettings files. The suite creates one host per test, so the default inotify
    /// instance limit (128) is exhausted on Linux before the suite finishes.
    /// Polling keeps the tests independent of the host OS limit.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
    }
}
