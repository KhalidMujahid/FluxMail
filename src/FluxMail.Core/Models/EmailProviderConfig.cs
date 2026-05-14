namespace FluxMail.Core.Models;

public enum ProviderType { Smtp, Resend, AwsSes, SendGrid, Mailgun }

public class EmailProviderConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ProviderType Type { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;

    public string SenderName { get; set; } = "";
    public string SenderEmail { get; set; } = "";

    public int ProviderWeight { get; set; } = 1;
    public int? DailySendingLimit { get; set; }
    public int? SendsPerMinute { get; set; }

    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; } = true;

    public string? ResendApiKey { get; set; }

    public string? AwsAccessKeyId { get; set; }
    public string? AwsSecretAccessKey { get; set; }
    public string? AwsRegion { get; set; } = "us-east-1";

    public string? SendGridApiKey { get; set; }

    public string? MailgunApiKey { get; set; }
    public string? MailgunDomain { get; set; }

    public string? PhysicalAddress { get; set; }
    public string? UnsubscribeBaseUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
