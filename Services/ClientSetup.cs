using System.Net.Http.Headers;

public static class ClientSetup
{
    public static HttpClient Create(string token)
    {
        var client = new HttpClient { BaseAddress = new Uri("https://brapi.dev/api/") };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

