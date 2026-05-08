namespace FluxMail.Core.Models;

public enum QueuePriority { High = 0, Normal = 1 }
public enum QueueItemStatus { Pending, Processing, Sent, Failed, Dead }

public class EmailQueueItem
{
    public int Id { get; set; }
    public string ToEmail { get; set; } = "";
    public string ToName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string HtmlBody { get; set; } = "";
    public string? PlainTextBody { get; set; }
    public int? PreferredProviderId { get; set; }
    public QueuePriority Priority { get; set; } = QueuePriority.Normal;
    public QueueItemStatus Status { get; set; } = QueueItemStatus.Pending;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string? LastError { get; set; }
    public string? TrackingId { get; set; }
    public int? CampaignId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
