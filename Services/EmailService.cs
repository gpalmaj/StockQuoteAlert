using MailKit.Net.Smtp;
using MimeKit;
using System.Text.Json;
using System.Text.Json.Nodes;


public class EmailService
{
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
    public static void SendEmail( SmtpSender sender, Receiver receiver, bool high, decimal currentPrice, string stock) // * true -> atingiu valor de venda || false -> atingiu valor de compra

    {
        var message = new MimeMessage();
        message.From.Add( new MailboxAddress("Stock Quote Alert", sender.Username ));
        message.To.Add( new MailboxAddress(" ", receiver.Email));

        var warning = $"Seu alerta de preço foi disparado! ";
        var suggestion = high?" VENDA ":" COMPRE ";

        message.Subject = warning + suggestion + stock;
        message.Body = new TextPart("plain")
        {
            Text = $"O valor da ação é de {currentPrice}"
        };

        using var client = new SmtpClient();
        client.Connect(sender.Host, sender.Port, false);
        client.Authenticate(sender.Username, sender.Password);
        client.Send(message);
        client.Disconnect(true);
    }
}