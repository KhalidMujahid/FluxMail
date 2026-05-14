namespace FluxMail.Core.Models;

public record EmailMessage
{
    public string ToEmail { get; init; } = "";
    public string ToName { get; init; } = "";
    public string Subject { get; init; } = "";
    public string HtmlBody { get; init; } = "";
    public string? PlainTextBody { get; init; }
    public string? ReplyTo { get; init; }
    public string? FromNameOverride { get; init; }
    public string? UnsubscribeUrl { get; init; }
}
