using FluxMail.Core.Models;

namespace FluxMail.Core.Interfaces;

public record EmailSendResult(bool Success, string? MessageId = null, string? ErrorMessage = null);

public interface IEmailProvider
{
    ProviderType ProviderType { get; }
    Task<bool> TestConnectionAsync(EmailProviderConfig config, CancellationToken ct = default);
    Task<EmailSendResult> SendAsync(EmailProviderConfig config, EmailMessage message, CancellationToken ct = default);
}
