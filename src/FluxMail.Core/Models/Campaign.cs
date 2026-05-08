namespace FluxMail.Core.Models;

public enum CampaignStatus { Draft, Scheduled, Sending, Completed, Failed }
public enum RecurrenceType { None, Daily, Weekly, Monthly }

public class Campaign
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Subject { get; set; } = "";
    public string HtmlBody { get; set; } = "";
    public string? PlainTextBody { get; set; }
    public string? FromNameOverride { get; set; }
    public int? ContactListId { get; set; }
    public int? ProviderId { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }

    public DateTime? ScheduledAt { get; set; }
    public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;
    public int RecurrenceInterval { get; set; } = 1;
    public DateTime? NextRunAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ContactList? ContactList { get; set; }
    public EmailProviderConfig? Provider { get; set; }
    public List<EmailLog> Logs { get; set; } = [];
}
