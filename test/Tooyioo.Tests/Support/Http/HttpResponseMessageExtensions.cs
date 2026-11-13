namespace Tooyioo.Tests.Support.Http;

internal static class HttpResponseMessageExtensions
{
    public static async Task<T> ReadJson<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<T>();
        return body ?? throw new InvalidOperationException($"Response body could not be read as {typeof(T).Name}.");
    }
}
