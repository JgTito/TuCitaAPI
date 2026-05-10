namespace TuCita.Infrastucture.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "TuCita";
    public int TimeoutSeconds { get; set; } = 30;
}
