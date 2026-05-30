
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

//Initialization of SMTP service
SmtpSender sender;
Receiver receiver;
try
{
    sender   = config.GetSection("SmtpSettings").Get<SmtpSender>()
            ?? throw new InvalidOperationException("SmtpSettings missing from appsettings.json");
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

var monitor = new Monitor(client, sender, receiver, appArgs);
return await monitor.RunAsync(cts.Token);
