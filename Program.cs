
using System.Globalization;
using System.Security.Authentication;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;


// configuring secrets
var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var token = config["brapiKey"];
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("brapi key not configured. Run: dotnet user-secrets");
    return 1;
}

// validating command line arguments
if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: dotnet run -- <STOCK> <SELLPRICE> <BUYPRICE>");
    return 1;
} 
string stock = args[0];


if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var upperLimit))
{
    Console.Error.WriteLine($"SELLPRICE must be a valid number. Got: {args[1]} instead");
    return 1;
}
if (!decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var lowerLimit))
{
    Console.Error.WriteLine($"BUYPRICE must be a valid number. Got: {args[2]} instead");
    return 1;
}


//Sanity checks
if (upperLimit <= 0 || lowerLimit <= 0)
{
    Console.Error.WriteLine("SELLPRICE and BUYPRICE must be positive numbers.");
    return 1;
}

if (lowerLimit >= upperLimit)
{
    Console.Error.WriteLine(
        $"BUYPRICE ({lowerLimit}) must be less than SELLPRICE ({upperLimit}).");
    return 1;
}

if (string.IsNullOrWhiteSpace(stock))
{
    Console.Error.WriteLine("STOCK cannot be empty.");
    return 1;
}


// Initialization of http client
var client = ClientSetup.Create(token) ;

//Initialization of SMTP service
SmtpSender sender;
Receiver receiver;
try
{
     (sender, receiver) = EmailService.Init("appsettings.json");
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

while (!cts.IsCancellationRequested)
{
    try
    {
        var price = await QuoteService.GetQuote(client, $"{stock}");
        Console.WriteLine(price);

    if (price > upperLimit)
    {
        EmailService.SendEmail(sender, receiver, true, price, stock);
    }
    else if (price < lowerLimit)
    {
        EmailService.SendEmail(sender, receiver, false, price, stock);
    }
    
        await Task.Delay(60000, cts.Token);

    }   
    catch(HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Unauthorized)
    {
        //Ends here because it will always go wrong
        Console.Error.WriteLine($"API error {ex.StatusCode}: {ex.Message}");
        return 1;
    }
    catch(HttpRequestException ex)
    {
        //Can be momentary so returns to loop
        Console.Error.WriteLine($"Transient API error {ex.StatusCode}: {ex.Message}");
    }
    catch(AuthenticationException ex)
    {
        Console.Error.WriteLine($"SMTP Auth failed: {ex.Message}");
        return 1;

    }
    catch (MailKit.Net.Smtp.SmtpCommandException ex)
    {
        Console.Error.WriteLine($"SMTP failed: {ex.Message}");
    }
    catch(OperationCanceledException) when (cts.IsCancellationRequested)
    {
        break;
    }
    catch (TaskCanceledException)
    {
        Console.Error.WriteLine("Request timed out");
    }


   
}

return 0;