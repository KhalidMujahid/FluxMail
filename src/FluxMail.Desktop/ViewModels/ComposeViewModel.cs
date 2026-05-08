using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Core.Services;
using FluxMail.Desktop.Services;

namespace FluxMail.Desktop.ViewModels;

public partial class ComposeViewModel : ViewModelBase, IAsyncLoadable
{
    private readonly EmailService _emailService;
    private readonly IProviderRepository _providerRepo;
    private readonly ITemplateRepository _templateRepo;
    private readonly SpamAnalyzer _spamAnalyzer;
    private readonly IFilePickerService _filePicker;
    private readonly NotificationService _notifications;
    private readonly IContactRepository _contactRepo;

    [ObservableProperty] private ObservableCollection<EmailProviderConfig> _providers = [];
    [ObservableProperty] private ObservableCollection<EmailTemplate> _templates = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FromName))]
    [NotifyPropertyChangedFor(nameof(FromEmail))]
    [NotifyPropertyChangedFor(nameof(HasFromInfo))]
    private EmailProviderConfig? _selectedProvider;

    [ObservableProperty] private string _fromNameOverride = "";
    [ObservableProperty] private string _toEmail = "";
    [ObservableProperty] private string _toName = "";
    [ObservableProperty] private string _subject = "";
    [ObservableProperty] private string _htmlBody = "";
    [ObservableProperty] private string _plainText = "";
    [ObservableProperty] private string _replyTo = "";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isSending;
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _includeUnsubscribe;

    // Editor mode
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPlainMode))]
    private bool _isHtmlMode = true;
    public bool IsPlainMode => !IsHtmlMode;

    // Image URL inline input
    [ObservableProperty] private string _imageUrlInput = "";
    [ObservableProperty] private bool _isImageUrlInputVisible;

    // Sender info from selected provider
    public string FromName => SelectedProvider?.SenderName ?? "";
    public string FromEmail => SelectedProvider?.SenderEmail ?? "";
    public bool HasFromInfo => !string.IsNullOrWhiteSpace(SelectedProvider?.SenderEmail);

    partial void OnSelectedProviderChanged(EmailProviderConfig? value) => FromNameOverride = "";

    // Spam check
    [ObservableProperty] private bool _spamChecked;
    [ObservableProperty] private double _spamScore;
    [ObservableProperty] private string _spamRisk = "";
    [ObservableProperty] private ObservableCollection<string> _spamIssues = [];

    // Bulk mode
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBulkMode))]
    private bool _isBulkMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BulkSendButtonText))]
    private string _bulkEmailsText = "";

    [ObservableProperty] private string _bulkRecipientSummary = "";
    [ObservableProperty] private bool _isBulkSending;
    [ObservableProperty] private int _bulkSentCount;
    [ObservableProperty] private double _bulkSendDelaySeconds = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BulkSendButtonText))]
    private int _bulkTotalCount;

    public bool IsNotBulkMode => !IsBulkMode;

    public string BulkSendButtonText => BulkTotalCount > 0
        ? $"Send to {BulkTotalCount} Recipients"
        : "Send Bulk";

    partial void OnBulkEmailsTextChanged(string value)
    {
        var recipients = ParseBulkRecipients(value);
        BulkTotalCount = recipients.Count;
        BulkRecipientSummary = recipients.Count > 0
            ? $"{recipients.Count} recipient{(recipients.Count == 1 ? "" : "s")} found"
            : "";
    }

    // Events — ComposeView opens windows / inserts text
    public event EventHandler<SendLogViewModel>? SendLogOpened;
    public event EventHandler<string>? ImageTagReady;

    public ComposeViewModel(
        EmailService emailService,
        IProviderRepository providerRepo,
        ITemplateRepository templateRepo,
        SpamAnalyzer spamAnalyzer,
        IFilePickerService filePicker,
        NotificationService notifications,
        IContactRepository contactRepo)
    {
        _emailService = emailService;
        _providerRepo = providerRepo;
        _templateRepo = templateRepo;
        _spamAnalyzer = spamAnalyzer;
        _filePicker = filePicker;
        _notifications = notifications;
        _contactRepo = contactRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        var providers = await _providerRepo.GetAllAsync();
        Providers = new ObservableCollection<EmailProviderConfig>(providers);
        SelectedProvider ??= providers.FirstOrDefault(p => p.IsDefault) ?? providers.FirstOrDefault();

        var templates = await _templateRepo.GetAllAsync();
        Templates = new ObservableCollection<EmailTemplate>(templates);
        IsLoading = false;
    }

    [RelayCommand] private void SetSingleMode() => IsBulkMode = false;
    [RelayCommand] private void SetBulkMode() => IsBulkMode = true;
    [RelayCommand] private void SetHtmlMode() => IsHtmlMode = true;
    [RelayCommand] private void SetPlainMode() => IsHtmlMode = false;

    // ── Image insertion ──────────────────────────────────────────────────────

    [RelayCommand] private void ShowImageUrlInput() => IsImageUrlInputVisible = true;
    [RelayCommand] private void CancelImageUrl() { ImageUrlInput = ""; IsImageUrlInputVisible = false; }

    [RelayCommand]
    private void InsertImageUrl()
    {
        if (string.IsNullOrWhiteSpace(ImageUrlInput)) { IsImageUrlInputVisible = false; return; }
        var tag = $"<img src=\"{ImageUrlInput.Trim()}\" alt=\"\" style=\"max-width:100%;height:auto;\"/>";
        ImageTagReady?.Invoke(this, tag);
        ImageUrlInput = "";
        IsImageUrlInputVisible = false;
    }

    [RelayCommand]
    private async Task UploadImageAsync()
    {
        var file = await _filePicker.ReadBinaryAsync("Select Image", ["png", "jpg", "jpeg", "gif", "webp", "svg"]);
        if (file is null) return;
        var mime = file.Value.Extension switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "gif"           => "image/gif",
            "webp"          => "image/webp",
            "svg"           => "image/svg+xml",
            _               => "image/png"
        };
        var base64 = Convert.ToBase64String(file.Value.Bytes);
        var tag = $"<img src=\"data:{mime};base64,{base64}\" alt=\"\" style=\"max-width:100%;height:auto;\"/>";
        ImageTagReady?.Invoke(this, tag);
    }

    // ── Templates ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ApplyTemplate(EmailTemplate template)
    {
        Subject = template.Subject;
        HtmlBody = template.HtmlBody;
        PlainText = template.PlainTextBody ?? "";
        IsHtmlMode = true;
        SpamChecked = false;
    }

    // ── Spam check ───────────────────────────────────────────────────────────

    [RelayCommand]
    private void CheckSpam()
    {
        if (string.IsNullOrWhiteSpace(Subject) && string.IsNullOrWhiteSpace(HtmlBody)) return;
        var result = _spamAnalyzer.Analyze(Subject, BuildEffectiveHtmlBody());
        SpamScore = result.Score;
        SpamRisk = result.Risk;
        SpamIssues = new ObservableCollection<string>(result.Issues);
        SpamChecked = true;
    }

    // ── Single send ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SendAsync()
    {
        if (SelectedProvider is null) { StatusMessage = "Please select a provider."; return; }
        if (string.IsNullOrWhiteSpace(ToEmail)) { StatusMessage = "Recipient email is required."; return; }
        if (string.IsNullOrWhiteSpace(Subject)) { StatusMessage = "Subject is required."; return; }
        if (string.IsNullOrWhiteSpace(HtmlBody)) { StatusMessage = "Email body is required."; return; }

        var listName = await _contactRepo.FindListNameForEmailAsync(ToEmail.Trim());
        if (listName is not null)
            _notifications.Info($"{ToEmail} is in contact list: {listName}");

        var log = new SendLogViewModel { Title = $"Sending to {ToEmail}" };
        SendLogOpened?.Invoke(this, log);

        IsSending = true;
        IsSuccess = false;
        StatusMessage = "Sending...";

        var effectiveFromName = string.IsNullOrWhiteSpace(FromNameOverride) ? FromName : FromNameOverride;
        log.Header("Email Send");
        log.Info($"Provider : {SelectedProvider.Name}");
        log.Info($"From     : {effectiveFromName} <{FromEmail}>");
        log.Info($"To       : {ToEmail}");
        log.Info($"Subject  : {Subject}");
        log.Info($"Mode     : {(IsHtmlMode ? "HTML Source" : "Rich Text")}");
        if (IncludeUnsubscribe) log.Info("Options  : Unsubscribe footer included");
        log.Separator();
        log.Info("Connecting to provider...");

        var message = new EmailMessage
        {
            ToEmail = ToEmail.Trim(),
            ToName = ToName.Trim(),
            Subject = Subject,
            HtmlBody = BuildEffectiveHtmlBody(),
            PlainTextBody = null,
            ReplyTo = string.IsNullOrWhiteSpace(ReplyTo) ? null : ReplyTo,
            FromNameOverride = string.IsNullOrWhiteSpace(FromNameOverride) ? null : FromNameOverride
        };

        var result = await _emailService.SendAsync(SelectedProvider.Id, message);
        IsSending = false;

        if (result.Success)
        {
            IsSuccess = true;
            StatusMessage = "Email sent successfully!";
            log.Success($"Delivered to {ToEmail}");
            log.Complete($"Sent successfully at {DateTime.Now:HH:mm:ss}");
            _notifications.Success($"Email sent to {ToEmail}");
            ClearForm();
        }
        else
        {
            StatusMessage = $"Failed: {result.ErrorMessage}";
            log.Fail(result.ErrorMessage ?? "Unknown error");
            log.Complete("Send failed.");
            _notifications.Error($"Send failed: {result.ErrorMessage}");
        }
    }

    // ── Bulk send ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task UploadTextFileAsync()
    {
        var content = await _filePicker.ReadTextAsync("Select Text File", ["txt"]);
        if (content is not null)
            BulkEmailsText = content;
    }

    [RelayCommand]
    private async Task UploadCsvFileAsync()
    {
        var content = await _filePicker.ReadTextAsync("Select CSV File", ["csv"]);
        if (content is null) return;
        var emails = ParseCsvEmails(content);
        BulkEmailsText = string.Join("\n", emails);
    }

    [RelayCommand]
    private async Task SendBulkAsync()
    {
        if (SelectedProvider is null) { StatusMessage = "Please select a provider."; return; }

        var recipients = ParseBulkRecipients(BulkEmailsText);
        if (recipients.Count == 0) { StatusMessage = "No valid email addresses found."; return; }
        if (string.IsNullOrWhiteSpace(Subject)) { StatusMessage = "Subject is required."; return; }
        if (string.IsNullOrWhiteSpace(HtmlBody)) { StatusMessage = "Email body is required."; return; }

        var log = new SendLogViewModel { Title = $"Bulk Send — {recipients.Count} recipients" };
        SendLogOpened?.Invoke(this, log);

        IsBulkSending = true;
        BulkSentCount = 0;
        BulkTotalCount = recipients.Count;
        StatusMessage = $"Sending 0 / {recipients.Count}...";
        IsSuccess = false;

        var defaultFromName = string.IsNullOrWhiteSpace(FromNameOverride) ? FromName : FromNameOverride;
        log.Header("Bulk Email Send");
        log.Info($"Provider   : {SelectedProvider.Name}");
        log.Info($"From       : {defaultFromName} <{FromEmail}>");
        log.Info($"Subject    : {Subject}");
        log.Info($"Recipients : {recipients.Count}");
        log.Info($"Mode       : {(IsHtmlMode ? "HTML Source" : "Rich Text")}");
        var delayMs = (int)(BulkSendDelaySeconds * 1000);
        log.Info($"Delay      : {(delayMs > 0 ? $"{BulkSendDelaySeconds:G}s between sends" : "none")}");
        if (IncludeUnsubscribe) log.Info("Options    : Unsubscribe footer included");
        log.Separator();

        var htmlBody = BuildEffectiveHtmlBody();
        var plainBody = IsHtmlMode
            ? (string.IsNullOrWhiteSpace(PlainText) ? null : PlainText)
            : PlainText;
        int success = 0, failed = 0;

        for (int i = 0; i < recipients.Count; i++)
        {
            var recipient = recipients[i];
            var contactName = await _contactRepo.FindNameForEmailAsync(recipient.Email);
            var displayTo = contactName is not null ? $"{contactName} <{recipient.Email}>" : recipient.Email;

            // Per-entry sender name: entry override → global override → provider default
            var perEntrySender = recipient.SenderName
                ?? (string.IsNullOrWhiteSpace(FromNameOverride) ? null : FromNameOverride);

            var senderDisplay = perEntrySender is not null ? $"{perEntrySender} <{FromEmail}>" : $"{FromName} <{FromEmail}>";
            log.Info($"[{i + 1:D4}/{recipients.Count}] → {displayTo}  (from: {senderDisplay})");

            if (i < 5)
            {
                var listName = await _contactRepo.FindListNameForEmailAsync(recipient.Email);
                if (listName is not null)
                    log.Info($"           ↳ in contact list: {listName}");
            }

            var message = new EmailMessage
            {
                ToEmail = recipient.Email,
                ToName = contactName ?? "",
                Subject = Subject,
                HtmlBody = htmlBody,
                PlainTextBody = plainBody,
                ReplyTo = string.IsNullOrWhiteSpace(ReplyTo) ? null : ReplyTo,
                FromNameOverride = perEntrySender
            };

            var result = await _emailService.SendAsync(SelectedProvider.Id, message);
            BulkSentCount++;

            if (result.Success) { success++; log.Success("Sent"); }
            else { failed++; log.Fail($"Failed — {result.ErrorMessage}"); }

            StatusMessage = $"Sending {BulkSentCount} / {recipients.Count}...";

            if (delayMs > 0 && i < recipients.Count - 1)
                await Task.Delay(delayMs);
        }

        IsBulkSending = false;
        IsSuccess = failed == 0;

        log.Separator();
        var summary = failed == 0
            ? $"All {success} emails delivered successfully."
            : $"Sent {success}, Failed {failed} of {recipients.Count}.";
        log.Info(summary);
        log.Complete(summary);

        StatusMessage = summary;

        if (failed == 0)
            _notifications.Success($"Bulk send complete: {success}/{recipients.Count} delivered");
        else
            _notifications.Warn($"Bulk send: {success} sent, {failed} failed");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string BuildHtmlBody(string html)
    {
        if (!IncludeUnsubscribe) return html;
        return html + """

            <div style="margin-top:32px;padding-top:16px;border-top:1px solid #e5e7eb;text-align:center;font-size:12px;color:#9ca3af;">
              <p>You're receiving this because you opted in to our mailing list.</p>
              <p><a href="#unsubscribe" style="color:#9ca3af;text-decoration:underline;">Unsubscribe</a> &nbsp;|&nbsp; <a href="#preferences" style="color:#9ca3af;text-decoration:underline;">Email Preferences</a></p>
            </div>
            """;
    }

    private static string WrapPlainAsHtml(string plain)
    {
        var escaped = plain
            .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\r\n", "<br/>").Replace("\n", "<br/>").Replace("\r", "<br/>");
        return $"<div style=\"font-family:sans-serif;white-space:pre-wrap;\">{escaped}</div>";
    }

    private string BuildEffectiveHtmlBody() => BuildHtmlBody(HtmlBody);

    private record BulkRecipient(string Email, string? SenderName);

    private static List<BulkRecipient> ParseBulkRecipients(string text)
    {
        var result = new List<BulkRecipient>();
        var lines = text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        int i = 0;
        while (i < lines.Count)
        {
            var line = lines[i];
            if (line.StartsWith("Sender name:", StringComparison.OrdinalIgnoreCase) && i + 1 < lines.Count)
            {
                var name = line["Sender name:".Length..].Trim();
                var next = lines[i + 1];
                if (next.StartsWith("Email:", StringComparison.OrdinalIgnoreCase))
                {
                    var email = next["Email:".Length..].Trim();
                    if (email.Contains('@') && email.Length > 3)
                        result.Add(new BulkRecipient(email, string.IsNullOrWhiteSpace(name) ? null : name));
                    i += 2;
                    continue;
                }
            }

            foreach (var e in line.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                                   .Select(e => e.Trim())
                                   .Where(e => e.Contains('@') && e.Length > 3))
                result.Add(new BulkRecipient(e, null));
            i++;
        }
        return [.. result.DistinctBy(r => r.Email)];
    }

    private static List<string> ParseCsvEmails(string csv)
    {
        var lines = csv.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return [];

        var headers = lines[0].Split(',')
                               .Select(h => h.Trim().Trim('"').ToLowerInvariant())
                               .ToArray();
        var emailColIdx = Array.FindIndex(headers, h => h.Contains("email"));

        if (emailColIdx < 0)
        {
            return [.. lines.SelectMany(l => l.Split(','))
                             .Select(c => c.Trim().Trim('"'))
                             .Where(c => c.Contains('@') && c.Length > 3)
                             .Distinct()];
        }

        return [.. lines.Skip(1)
                        .Select(l =>
                        {
                            var cols = l.Split(',');
                            return emailColIdx < cols.Length ? cols[emailColIdx].Trim().Trim('"') : "";
                        })
                        .Where(e => e.Contains('@') && e.Length > 3)
                        .Distinct()];
    }

    private void ClearForm()
    {
        ToEmail = ToName = Subject = HtmlBody = PlainText = ReplyTo = FromNameOverride = "";
        ImageUrlInput = "";
        IsImageUrlInputVisible = false;
        SpamChecked = false;
    }
}
