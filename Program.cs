
using System.Globalization;
using Microsoft.Extensions.Configuration;
using System.Text.Json;


// configuring secrets
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddUserSecrets<Program>()
    .Build();
var token = config["brapiKey"];
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("brapi key not configured. Run: dotnet user-secrets");
    return 1;
}

// validating command line arguments
if(!AppArgs.ParseArgs(args, out var appArgs, out var error))
{
    Console.Error.WriteLine(error);
    return 1;
}

// Initialization of http client
var client = ClientSetup.Create(token) ;
var previousZone = Zone.Within;

//Initialization of SMTP service
SmtpSender sender;
Receiver receiver;
try
{
     sender   = config.GetSection("SmtpSettings").Get<SmtpSender>()
               ?? throw new InvalidOperationException("SmtpSettings missing from appsetings.json");
     receiver = config.GetSection("Receiver").Get<Receiver>()
               ?? throw new InvalidOperationException("Receiver missing from appsettings.json");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to initialize email exchange: {ex.Message}");
    return 1;
}

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("Shutting down");
    cts.Cancel();
};

string tzId = OperatingSystem.IsWindows()? "E. South America Standard Time" : "America/Sao_Paulo";
TimeZoneInfo brZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
DateTime brTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brZone);
string timeStamp = brTime.ToString("[HH:mm]");

Console.WriteLine($"Monitoring {appArgs.Stock} -- Starting {timeStamp}");

while (!cts.IsCancellationRequested)
{
    try
    {

    brTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brZone);
    timeStamp = brTime.ToString("[HH:mm]");
    Console.Write($"{timeStamp} ");

    var price = await QuoteService.GetQuote(client, appArgs.Stock, cts.Token);
    if (price is null)
        {
            Console.WriteLine("Quote unavailable");
            await Task.Delay(60000, cts.Token);
            continue;

        }
    
    Console.Write($"{price} ");
    var currentZone = (price>appArgs.SellPrice) ? Zone.Above : (price<appArgs.BuyPrice) ? Zone.Below : Zone.Within;
    if(currentZone != previousZone)
        {
        if (currentZone == Zone.Above)
        {
            Console.Write(" - surpassed sell price ");
            await EmailService.SendAlertEmail(sender, receiver,EmailService.Alert.Sell , price.Value, appArgs.Stock, cts.Token);
        }
        else if (currentZone == Zone.Below)
        {
            Console.Write(" - below buy price ");
            await EmailService.SendAlertEmail(sender, receiver,EmailService.Alert.Buy , price.Value, appArgs.Stock, cts.Token);
        }
        previousZone = currentZone;
        }
    Console.WriteLine(";");
    await Task.Delay(60000, cts.Token);

    }   
    catch(HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
    {
        //Ends here because it will always go wrong
        Console.Error.WriteLine($"\nStock {appArgs.Stock} not found, check inputted symbol");
        return 1;
    }
    catch(HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized)
    {
        Console.Error.WriteLine("\nAPI key rejected. Check your brapi key configuration.");
        return 1;
        
    }
    catch(HttpRequestException ex)
    {
        //Can be momentary so returns to loop
        Console.Error.WriteLine($"\nTransient API error {ex.StatusCode}: {ex.Message}");
    }
    catch(EmailAuthException ex)
    {
        Console.Error.WriteLine($"\nEmail Auth failed: {ex.Message}");
        return 1;
    }
    catch(EmailTransientException ex)
    {
        Console.Error.WriteLine($"\nEmail send failed: {ex.Message}");
    }
    catch(OperationCanceledException) when (cts.IsCancellationRequested)
    {
        break;
    }
    catch (TaskCanceledException) when (!cts.IsCancellationRequested)
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
    catch( Exception ex)
    {
    Console.Error.WriteLine($"\nUnexpected error ({ex.GetType().FullName}): {ex.Message}");
    }
}

Console.WriteLine($"Monitoring stopped -- {timeStamp}");
return 0;

public enum Zone
{
    Within,
    Above,
    Below
}