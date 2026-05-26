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

var client = ClientSetup.Create(token);
var json = await QuoteService.GetQuote(client, $"{args[0]}");

Console.WriteLine(json);

return 0;
