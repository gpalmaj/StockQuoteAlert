using System.Text.Json;

public class QuoteService{
    public static async Task<decimal?> GetQuote(HttpClient client, string stock, CancellationToken ct = default)
    {

        using var response = await client.GetAsync($"quote/{stock}", ct);
        response.EnsureSuccessStatusCode();


        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc =  await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!doc.RootElement.TryGetProperty("results", out var results)
            || results.ValueKind != JsonValueKind.Array
            || results.GetArrayLength() == 0)
            return null;
        
        if (!results[0].TryGetProperty("regularMarketPrice", out var price)
            || price.ValueKind == JsonValueKind.Null)
            return null;

        return  price.TryGetDecimal(out var value)? value : null;
    }
}
