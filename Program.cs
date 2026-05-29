
using Microsoft.Extensions.Configuration;

// configuring secrets
var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var token = config["brapiKey"];

// validating command line arguments
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: StockQuoteAlert <TICKER> <SELLPRICE> <BUYPRICE>");
    return 1;
} 
string stock = args[0];
decimal lowerLimit = System.Convert.ToDecimal(args[1]);
decimal upperLimit = System.Convert.ToDecimal(args[2]);


// Initialization of http client
var client = ClientSetup.Create(token!) ;
var price = await QuoteService.GetQuote(client, $"{args[0]}");

//Initialization of SMTP service
var emailConfig = EmailService.Init("appsettings.json");
if (emailConfig is null)
{
    Console.WriteLine("Failed to inicialize email exchange");
    return 1;
}
var (sender, receiver) = emailConfig.Value;

EmailService.SendEmail(sender, receiver, false, 14.5m,0,  "ABEV3");

while (true)
{
    price = await QuoteService.GetQuote(client, $"{args[0]}");
    if (price > upperLimit)
    {
        EmailService.SendEmail(sender, receiver, true, upperLimit, price, stock);
    }
    else if (price < lowerLimit)
    {
        EmailService.SendEmail(sender, receiver, false, lowerLimit, price, stock);
    }
    Thread.Sleep(60000);
}

