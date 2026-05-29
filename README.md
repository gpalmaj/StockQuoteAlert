# StockQuoteAlert

A console application written in C# that monitors the price of a B3 (Brazilian stock exchange) asset and sends e-mail alerts whenever the quote crosses a configured upper or lower threshold.

The project was built as part of an internship selection challenge. It focuses on the minimum requirements with a clean separation of concerns and pragmatic fault tolerance around the two failure-prone boundaries: the external HTTP API and the SMTP server.

## Requirements

- .NET 10 SDK
- A free [brapi.dev](https://brapi.dev/docs) API token (used to query the B3 quote)
- An SMTP account with credentials (the project was developed and tested against Gmail SMTP using an app password)

## Project Structure

```
StockQuoteAlert/
├── Program.cs                  Entry point. Argument parsing, polling loop, error orchestration.
├── Models/
│   └── AlertParticipants.cs    POCOs that bind the appsettings.json sections.
├── Services/
│   ├── ClientSetup.cs          Builds the pre-configured HttpClient for brapi.
│   ├── QuoteService.cs         Performs the quote HTTP call and extracts the price.
│   └── EmailService.cs         Loads e-mail config and sends messages via MailKit.
├── appsettings.json            Receiver + SMTP configuration (see below).
└── StockQuoteAlert.csproj
```

The three logical modules of the design are:

- **Query** ([Services/QuoteService.cs](Services/QuoteService.cs), supported by [Services/ClientSetup.cs](Services/ClientSetup.cs)) gets the current `regularMarketPrice` from brapi.
- **Notification** ([Services/EmailService.cs](Services/EmailService.cs)) initialises the SMTP sender and recipient from the configuration file and dispatches the alert message.
- **Main** ([Program.cs](Program.cs)) wires everything together, validates input, and drives the polling loop.

## Configuration

### appsettings.json

The configuration file lives in the project root and must follow exactly the shape expected by the classes in [Models/AlertParticipants.cs](Models/AlertParticipants.cs). All fields are required.

```json
{
    "Receiver": {
        "Email": "destination@example.com"
    },
    "SmtpSettings": {
        "Host": "smtp.gmail.com",
        "Port": 587,
        "Username": "your.account@gmail.com",
        "Password": "your-smtp-app-password"
    }
}
```

| Section | Field | Description |
|---|---|---|
| `Receiver` | `Email` | Address that will receive the buy/sell alerts. |
| `SmtpSettings` | `Host` | SMTP server hostname (e.g. `smtp.gmail.com`). |
| `SmtpSettings` | `Port` | SMTP port. `587` is used with STARTTLS. |
| `SmtpSettings` | `Username` | SMTP account used to authenticate and as the message sender. |
| `SmtpSettings` | `Password` | SMTP password. For Gmail, generate an app password. |

If the file is missing, malformed, or any of the two sections cannot be deserialized, the program prints a descriptive error and exits before entering the polling loop.

### brapi API token (user secrets)

The brapi token is intentionally kept out of `appsettings.json` and out of source control. It is read from the .NET user-secrets store under the key `brapiKey`:

```bash
dotnet user-secrets init
dotnet user-secrets set "brapiKey" "your-brapi-token"
```

If the secret is not configured, the program exits immediately with an error.

## Usage

Build and run the project, passing three positional arguments:

```bash
dotnet run -- <STOCK> <SELLPRICE> <BUYPRICE>
```

| Argument | Description |
|---|---|
| `STOCK` | Ticker on B3 (e.g. `PETR4`, `VALE3`, `ITUB4`). |
| `SELLPRICE` | Upper threshold. When the quote rises above this value, a *sell* alert is sent. |
| `BUYPRICE` | Lower threshold. When the quote falls below this value, a *buy* alert is sent. |

Example:

```bash
dotnet run -- PETR4 38.50 32.10
```

Numeric arguments are parsed with `CultureInfo.InvariantCulture`, so the decimal separator is always a dot, regardless of the host machine locale.

The program performs the following sanity checks before starting:

- `STOCK` must not be empty.
- `SELLPRICE` and `BUYPRICE` must be valid positive numbers.
- `BUYPRICE` must be strictly less than `SELLPRICE`.

Any failed check prints a clear message and exits with status code `1`.

## How it works

Once initialised, the program enters an infinite loop that polls brapi every 60 seconds:

1. Query the current price of `STOCK` from `https://brapi.dev/api/quote/{STOCK}`.
2. If `price > SELLPRICE`, send a *sell* recommendation e-mail.
3. If `price < BUYPRICE`, send a *buy* recommendation e-mail.
4. Otherwise do nothing and wait for the next tick.

The interpretation of the requirement was deliberate: the challenge statement says *"every time the price is greater than the blue line, an e-mail must be fired"*, so an alert is sent on **every** out-of-band poll, not only on the transition. Switching to edge-triggered alerts would only require tracking the previous state.

The polling cadence is set to one query per minute, well within brapi's free-tier limits.

