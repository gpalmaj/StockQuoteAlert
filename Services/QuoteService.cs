using System.Text.Json;

public class QuoteService{

    // 
    public static async Task<decimal> GetQuote(HttpClient client, string stock)
    {
        var response = await client.GetAsync($"quote/{stock}");
        response.EnsureSuccessStatusCode();
        var doc = JsonDocument.Parse( await response.Content.ReadAsStringAsync());

        return  doc.RootElement.GetProperty("results")[0].GetProperty("regularMarketPrice").GetDecimal();
    }
}
