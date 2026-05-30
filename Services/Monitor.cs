using System.Text.Json;

public class StockMonitor(HttpClient client, SmtpSender sender, Receiver receiver, AppArgs args)
{
    public enum Zone
{
    Within,
    Above,
    Below
}
    private readonly TimeZoneInfo _brZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");

    public async Task<int> RunAsync(CancellationToken ct)
    {
        var previousZone = Zone.Within;
        string timeStamp = Timestamp();

        Console.WriteLine($"Monitoring {args.Stock} -- Starting {timeStamp}");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                timeStamp = Timestamp();
                Console.Write($"{timeStamp} ");

                var price = await QuoteService.GetQuote(client, args.Stock, ct);
                if (price is null)
                {
                    Console.WriteLine("Quote unavailable");
                    await Task.Delay(60000, ct);
                    continue;
                }

                Console.Write($"{price} ");
                var currentZone = (price > args.SellPrice) ? Zone.Above
                                : (price < args.BuyPrice)  ? Zone.Below
                                                           : Zone.Within;

                if (currentZone != previousZone)
                {
                    if (currentZone == Zone.Above)
                    {
                        Console.Write(" - surpassed sell price ");
                        await EmailService.SendAlertEmail(sender, receiver, EmailService.Alert.Sell, price.Value, args.Stock, ct);
                    }
                    else if (currentZone == Zone.Below)
                    {
                        Console.Write(" - below buy price ");
                        await EmailService.SendAlertEmail(sender, receiver, EmailService.Alert.Buy, price.Value, args.Stock, ct);
                    }
                    previousZone = currentZone;
                }

                Console.WriteLine(";");
                await Task.Delay(60000, ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
            {
                // Ends here because it will always go wrong
                Console.Error.WriteLine($"\nStock {args.Stock} not found, check inputted symbol");
                return 1;
            }
            catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized)
            {
                Console.Error.WriteLine("\nAPI key rejected. Check your brapi key configuration.");
                return 1;
            }
            catch (HttpRequestException ex)
            {
                // Can be momentary so returns to loop
                Console.Error.WriteLine($"\nTransient API error {ex.StatusCode}: {ex.Message}");
            }
            catch (EmailAuthException ex)
            {
                Console.Error.WriteLine($"\nEmail Auth failed: {ex.Message}");
                return 1;
            }
            catch (EmailTransientException ex)
            {
                Console.Error.WriteLine($"\nEmail send failed: {ex.Message}");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                Console.Error.WriteLine("\nRequest timed out");
            }
            catch (JsonException)
            {
                Console.Error.WriteLine("\nMalformed API response");
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"\nNetwork error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nUnexpected error ({ex.GetType().FullName}): {ex.Message}");
            }
        }

        Console.WriteLine($"Monitoring stopped -- {timeStamp}");
        return 0;
    }

    private string Timestamp()
    {
        var brTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _brZone);
        return brTime.ToString("[HH:mm]");
    }
}