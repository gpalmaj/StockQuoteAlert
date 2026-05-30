using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Globalization;


public record AppArgs(string Stock, decimal SellPrice, decimal BuyPrice)
{
    public static bool ParseArgs(string[] args,  [NotNullWhen(true)] out AppArgs? result, [NotNullWhen(false)] out string? error)
    {
        result = null;

        //program must receive 3 arguments
        if (args.Length < 3)
        {
            error = "Usage: dotnet run -- <STOCK> <SELLPRICE> <BUYPRICE>";
            return false;
        }
        var stock = args[0];
        //checking correctness
        if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var sellPrice))
        {
            error =$"SELLPRICE must be a valid number. Got: {args[1]} instead";
            return false;
        }
        if (!decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var buyPrice))
        {
            error =$"BUYPRICE must be a valid number. Got: {args[2]} instead";
            return false;
        }

        //checking sanity
        if (sellPrice <= 0 || buyPrice <= 0)
        {
            error = "SELLPRICE and BUYPRICE must be positive numbers.";
            return false;
        }

        if (buyPrice >= sellPrice)
        {
            error = $"BUYPRICE ({buyPrice}) must be less than SELLPRICE ({sellPrice}).";
            return false;
        }

        result = new AppArgs(stock, buyPrice, sellPrice);
        error = null;

        return true;
    }
}