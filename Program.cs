
using System.Globalization;
using Microsoft.Extensions.Configuration;

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
    Console.Error.WriteLine("Usage: StockQuoteAlert <STOCK> <SELLPRICE> <BUYPRICE>");
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
    Console.Error.WriteLine($"SELLPRICE must be a valid number. Got: {args[2]} instead");
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
var client = ClientSetup.Create(token!) ;

//Initialization of SMTP service
var emailConfig = EmailService.Init("appsettings.json");
if (emailConfig is null)
{
    Console.WriteLine("Failed to initialize email exchange");
    return 1;
}
var (sender, receiver) = emailConfig.Value;

//TODO Graceful shutdown

while (true)
{
    try
    {
        var price = await QuoteService.GetQuote(client, $"{stock}");
        Console.WriteLine(price);
        Console.WriteLine(upperLimit);
        Console.WriteLine(lowerLimit);


    if (price > upperLimit)
    {
        EmailService.SendEmail(sender, receiver, true, price, stock);
    }
    else if (price < lowerLimit)
    {
        EmailService.SendEmail(sender, receiver, false, price, stock);
    }
    

    }
    catch(Exception ex)
    {
        Console.Error.Write($"Failed to fetch quote: {ex.Message}");
    }
   
    await Task.Delay(15000);
}

