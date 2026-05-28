public class Receiver
{
    required public string Email {get; set;}
}

public class SmtpSender
{
    required public string Host {get; set;}
    required public int Port {get; set;}
    required public string Username {get; set;}
    required public string Password { get; set;}    
}