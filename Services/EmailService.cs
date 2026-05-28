using MailKit.Net.Smtp;
using MimeKit;

public class EmailService
{
    public static Boolean SendEmail( SmtpSender sender, Reciever reciever, bool high, decimal price, string stock) // * true -> atingiu valor de venda || false -> atingiu valor de compra

    {
        var message = new MimeMessage();
        message.From.Add( new MailboxAddress("Stock Quote Alert", sender.Username ));
        message.To.Add( new MailboxAddress("User da Silva", reciever.Email));

        var warning = $"!!!Atenção!!! A Ação {stock} atingiu o valor {price}";
        var sujestion = high?" VENDA!":" COMPRE!";

        message.Subject = warning + sujestion;
        message.Body = new TextPart("plain")
        {
            Text = """
            Seu alerta de preço foi disparado.
            """
        };

        using var client = new SmtpClient();
        client.Connect(sender.Host, sender.Port, false);
        client.Authenticate(sender.Username, sender.Password);
        client.Send(message);
        client.Disconnect(true);
        
        return true;
    }
}