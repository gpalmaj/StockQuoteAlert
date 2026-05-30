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
├── Program.cs                  Entry point. Loads configuration, wires dependencies, installs the Ctrl+C handler, hands control to StockMonitor.
├── Models/
│   └── AlertParticipants.cs    POCOs that bind the appsettings.json sections (Receiver, SmtpSender).
├── Services/
│   ├── AppArgs.cs              Record + parser for the three command-line arguments. Owns input validation.
│   ├── ClientSetup.cs          Builds the pre-configured HttpClient for brapi (base address, bearer token, timeout).
│   ├── QuoteService.cs         Performs the quote HTTP call and extracts regularMarketPrice from the response.
│   ├── EmailService.cs         Sends alert messages via MailKit. Wraps SMTP failures in EmailAuthException / EmailTransientException.
│   └── StockMonitor.cs         Polling loop, zone classification, error orchestration, and timestamped output.
├── appsettings.json            Receiver + SMTP configuration (see below).
└── StockQuoteAlert.csproj
```

The four logical modules of the design are:

- **Bootstrap** ([Program.cs](Program.cs)) loads configuration and the brapi user secret, parses the CLI arguments via `AppArgs.ParseArgs`, binds the SMTP/Receiver sections, wires the `CancellationToken` to `Ctrl+C`, and hands control to `StockMonitor`.
- **Query** ([Services/QuoteService.cs](Services/QuoteService.cs), supported by [Services/ClientSetup.cs](Services/ClientSetup.cs)) gets the current `regularMarketPrice` from brapi.
- **Notification** ([Services/EmailService.cs](Services/EmailService.cs)) dispatches the alert message and translates MailKit failures into the typed `EmailAuthException` (fatal) / `EmailTransientException` (recoverable) so the loop can react appropriately.
- **Monitor** ([Services/StockMonitor.cs](Services/StockMonitor.cs)) runs the polling loop: queries the price, classifies it into a `Zone` (`Above` / `Below` / `Within`), fires the alert on edge transitions, prints a timestamped line per tick, and decides whether each exception is fatal or transient.

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

Once initialised, the program enters a polling loop that queries brapi every 60 seconds:

1. Query the current price of `STOCK` from `https://brapi.dev/api/quote/{STOCK}`.
2. Classify the price into one of three zones: `Above` (`> SELLPRICE`), `Below` (`< BUYPRICE`), or `Within`.
3. If the zone **changed** since the previous tick:
   - Entering `Above` → send a *sell* recommendation e-mail.
   - Entering `Below` → send a *buy* recommendation e-mail.
4. Otherwise wait for the next tick.

Alerts are **edge-triggered**: one e-mail is sent when the price first crosses a threshold, and another only after the price re-enters the band and crosses again. The challenge statement is phrased as if every out-of-band poll should fire a message, but a literal level-triggered implementation would flood the inbox while the price sits outside the band, so the edge-triggered semantics were chosen instead.

The polling cadence is set to one query per minute, well within brapi's free-tier limits.

### Runtime output

Each tick prints a line to stdout, timestamped in `America/Sao_Paulo` time:

```
[14:32] 38.71 ;
[14:33] 38.74 ;
[14:34] 38.92  - surpassed sell price ;
[14:35] 38.95 ;
```

The `surpassed sell price` / `below buy price` annotation is printed on the same tick the alert e-mail is dispatched.

### Graceful shutdown

`Ctrl+C` is intercepted: the program cancels the pending HTTP/SMTP work, prints `Monitoring stopped -- [HH:mm]`, and exits with status `0`. The same `CancellationToken` is threaded through the brapi request, the JSON parsing, and the MailKit SMTP calls.

### Fault tolerance

The polling loop distinguishes between fatal and transient failures:

| Condition | Behaviour |
|---|---|
| brapi `404 Not Found` (unknown ticker) | Exit `1` — the symbol will never resolve. |
| brapi `401 Unauthorized` (bad API key) | Exit `1` — the key needs to be reconfigured. |
| SMTP authentication failure | Exit `1` — credentials in `appsettings.json` are wrong. |
| Other brapi HTTP errors, request timeouts, malformed JSON, I/O errors | Log and continue.  |
| Transient SMTP send failure (`SmtpCommandException`, `SmtpProtocolException`, `IOException`) | Log and continue. |
| Quote payload missing `results` / `regularMarketPrice` | Print `Quote unavailable` and wait. |

