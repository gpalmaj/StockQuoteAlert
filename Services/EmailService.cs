using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text.Json;
using System.Text.Json.Nodes;

public class EmailAuthException : Exception
{
    public EmailAuthException(string message, Exception inner) : base(message, inner) { }
}

public class EmailTransientException : Exception
{
    public EmailTransientException(string message, Exception inner) : base(message, inner) { }
}

public class EmailService
{
    public enum Alert{ Sell, Buy}

    public static (SmtpSender, Receiver) Init(string fileName)
    {
        string jsonString = File.ReadAllText(fileName);
        var node = JsonNode.Parse(jsonString)
            ?? throw new InvalidOperationException($"'{fileName} is empty or invalid JSON");

        var sender = node["SmtpSettings"]?.Deserialize<SmtpSender>()
            ?? throw new InvalidOperationException($"'SmtpSettings' section not found in {fileName}");
        var receiver = node["Receiver"]?.Deserialize<Receiver>()
            ?? throw new InvalidOperationException($"'Receiver' section not found in {fileName}");;
        return (sender, receiver);
        
    }
    
    public static async  Task SendAlertEmail(SmtpSender sender, Receiver receiver, Alert kind, decimal currentPrice, string stock, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add( new MailboxAddress("Stock Quote Alert", sender.Username ));
        message.To.Add( new MailboxAddress(" ", receiver.Email));
        var suggestion = kind == Alert.Sell ? "VENDA " : "COMPRE ";
        message.Subject = $"{suggestion} {stock}";
        message.Body = new TextPart("plain")
        {
            Text = $"Seu alerta de preço para {stock} foi disparado.\nA ação atingiu o preço de R${currentPrice}"
        };

        using var client = new SmtpClient();
        try
        {    
        await client.ConnectAsync(sender.Host, sender.Port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(sender.Username, sender.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            throw new EmailAuthException("SMTP Authentication Failed, check appsetings.json", ex);
        }
        catch (Exception ex) when ( ex is SmtpCommandException || ex is SmtpProtocolException || ex is IOException)
        {
            throw new EmailTransientException($"Failed to send Email: {ex.Message}", ex);
        }

    }
}