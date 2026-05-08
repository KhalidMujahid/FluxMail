namespace FluxMail.Core.Models;

public enum EmailStatus { Sent, Failed }

public class EmailLog
{
    public int Id { get; set; }
    public string ToEmail { get; set; } = "";
    public string ToName { get; set; } = "";
    public string Subject { get; set; } = "";
    public int? CampaignId { get; set; }
    public int? ProviderId { get; set; }
    public EmailStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public string? TrackingId { get; set; }
    public int OpenCount { get; set; }
    public int ClickCount { get; set; }

    public Campaign? Campaign { get; set; }
}
