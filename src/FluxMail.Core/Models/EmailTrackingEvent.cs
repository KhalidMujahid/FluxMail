namespace FluxMail.Core.Models;

public enum TrackingEventType { Open, Click }

public class EmailTrackingEvent
{
    public int Id { get; set; }
    public string TrackingId { get; set; } = "";
    public int? EmailLogId { get; set; }
    public int? CampaignId { get; set; }
    public TrackingEventType EventType { get; set; }
    public string? ClickedUrl { get; set; }
    public string? UserAgent { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
