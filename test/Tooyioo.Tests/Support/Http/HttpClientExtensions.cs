using System.Net.Http.Headers;

// ReSharper disable ConvertToExtensionBlock

namespace Tooyioo.Tests.Support.Http;

internal static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> Get(
        this HttpClient client,
        string uri,
        string? bearerToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, uri);
        return await HttpResponseMessage(client, bearerToken, message);
    }

    public static async Task<HttpResponseMessage> PostJson(
        this HttpClient client,
        string uri,
        object request,
        string? bearerToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, uri);
        message.Content = JsonContent.Create(request);
        return await HttpResponseMessage(client, bearerToken, message);
    }

    private static async Task<HttpResponseMessage> HttpResponseMessage(
        HttpClient client, 
        string? bearerToken, 
        HttpRequestMessage message)
    {
        if (bearerToken is not null)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await client.SendAsync(message);
    }
}
