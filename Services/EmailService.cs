using MailKit.Net.Smtp;
using MimeKit;
using System.Text.Json;
using System.Text.Json.Nodes;


public class EmailService
{
    public static (SmtpSender, Receiver)? Init(string fileName)
    {
        string jsonString = File.ReadAllText(fileName);
        var node = JsonNode.Parse(jsonString);
        if (node is null) return null;

        SmtpSender sender = node["SmtpSettings"]?.Deserialize<SmtpSender>()!;
        Receiver receiver = node["Receiver"]?.Deserialize<Receiver>()!;
        if ((sender is null) || (receiver is null)) return null;
        return (sender, receiver);
        
    }
    public static void SendEmail( SmtpSender sender, Receiver receiver, bool high, decimal priceLimit, decimal currentPrice, string stock) // * true -> atingiu valor de venda || false -> atingiu valor de compra

    {
        var message = new MimeMessage();
        message.From.Add( new MailboxAddress("Stock Quote Alert", sender.Username ));
        message.To.Add( new MailboxAddress("User da Silva", receiver.Email));

        var warning = $"Seu alerta de preço foi disparado! ";
        var sujestion = high?" VENDA ":" COMPRE ";

        message.Subject = warning + sujestion + stock;
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