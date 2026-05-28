public class Reciever
{
    public string Email {get; set;}
}

public class SmtpSender
{
    public string Host {get; set;}
    public int Port {get; set;}
    public string Username {get; set;}
    public string Password { get; set;}
    public bool EnableSSL {get; set;}
    
}