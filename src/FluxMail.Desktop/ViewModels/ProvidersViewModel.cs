using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Core.Services;

namespace FluxMail.Desktop.ViewModels;

public partial class ProvidersViewModel : ViewModelBase, IAsyncLoadable
{
    private readonly IProviderRepository _repo;
    private readonly EmailService _emailService;

    [ObservableProperty] private ObservableCollection<EmailProviderConfig> _providers = [];
    [ObservableProperty] private EmailProviderConfig? _selectedProvider;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isLoading;

    // Form — common
    [ObservableProperty] private string _formName = "";
    [ObservableProperty] private string _formSenderName = "";
    [ObservableProperty] private string _formSenderEmail = "";
    [ObservableProperty] private ProviderType _formType = ProviderType.Smtp;
    [ObservableProperty] private bool _formIsEnabled = true;
    [ObservableProperty] private int _formWeight = 1;
    [ObservableProperty] private string _formDailyLimit = "";
    [ObservableProperty] private string _formSendsPerMinute = "";

    // SMTP
    [ObservableProperty] private string _formSmtpHost = "";
    [ObservableProperty] private int _formSmtpPort = 587;
    [ObservableProperty] private string _formSmtpUsername = "";
    [ObservableProperty] private string _formSmtpPassword = "";
    [ObservableProperty] private bool _formSmtpUseSsl = true;

    // Resend
    [ObservableProperty] private string _formResendApiKey = "";

    // AWS SES
    [ObservableProperty] private string _formAwsAccessKeyId = "";
    [ObservableProperty] private string _formAwsSecretAccessKey = "";
    [ObservableProperty] private string _formAwsRegion = "us-east-1";

    // SendGrid
    [ObservableProperty] private string _formSendGridApiKey = "";

    // Mailgun
    [ObservableProperty] private string _formMailgunApiKey = "";
    [ObservableProperty] private string _formMailgunDomain = "";

    // Compliance
    [ObservableProperty] private string _formPhysicalAddress = "";
    [ObservableProperty] private string _formUnsubscribeBaseUrl = "";

    public ProviderType[] ProviderTypes { get; } = Enum.GetValues<ProviderType>();

    public bool IsSmtpType => FormType == ProviderType.Smtp;
    public bool IsResendType => FormType == ProviderType.Resend;
    public bool IsSesType => FormType == ProviderType.AwsSes;
    public bool IsSendGridType => FormType == ProviderType.SendGrid;
    public bool IsMailgunType => FormType == ProviderType.Mailgun;

    partial void OnFormTypeChanged(ProviderType value)
    {
        OnPropertyChanged(nameof(IsSmtpType));
        OnPropertyChanged(nameof(IsResendType));
        OnPropertyChanged(nameof(IsSesType));
        OnPropertyChanged(nameof(IsSendGridType));
        OnPropertyChanged(nameof(IsMailgunType));
    }

    public ProvidersViewModel(IProviderRepository repo, EmailService emailService)
    {
        _repo = repo;
        _emailService = emailService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        var list = await _repo.GetAllAsync();
        Providers = new ObservableCollection<EmailProviderConfig>(list);
        IsLoading = false;
    }

    [RelayCommand]
    private void NewProvider()
    {
        SelectedProvider = null;
        ClearForm();
        IsEditing = true;
    }

    [RelayCommand]
    private void EditProvider(EmailProviderConfig provider)
    {
        SelectedProvider = provider;
        FormName = provider.Name;
        FormSenderName = provider.SenderName;
        FormSenderEmail = provider.SenderEmail;
        FormType = provider.Type;
        FormIsEnabled = provider.IsEnabled;
        FormWeight = provider.ProviderWeight;
        FormDailyLimit = provider.DailySendingLimit?.ToString() ?? "";
        FormSendsPerMinute = provider.SendsPerMinute?.ToString() ?? "";

        FormSmtpHost = provider.SmtpHost ?? "";
        FormSmtpPort = provider.SmtpPort;
        FormSmtpUsername = provider.SmtpUsername ?? "";
        FormSmtpPassword = provider.SmtpPassword ?? "";
        FormSmtpUseSsl = provider.SmtpUseSsl;

        FormResendApiKey = provider.ResendApiKey ?? "";

        FormAwsAccessKeyId = provider.AwsAccessKeyId ?? "";
        FormAwsSecretAccessKey = provider.AwsSecretAccessKey ?? "";
        FormAwsRegion = provider.AwsRegion ?? "us-east-1";

        FormSendGridApiKey = provider.SendGridApiKey ?? "";

        FormMailgunApiKey = provider.MailgunApiKey ?? "";
        FormMailgunDomain = provider.MailgunDomain ?? "";

        FormPhysicalAddress = provider.PhysicalAddress ?? "";
        FormUnsubscribeBaseUrl = provider.UnsubscribeBaseUrl ?? "";

        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveProviderAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName) || string.IsNullOrWhiteSpace(FormSenderEmail))
        {
            StatusMessage = "Name and sender email are required.";
            return;
        }

        var config = SelectedProvider ?? new EmailProviderConfig();
        config.Name = FormName;
        config.SenderName = FormSenderName;
        config.SenderEmail = FormSenderEmail;
        config.Type = FormType;
        config.IsEnabled = FormIsEnabled;
        config.ProviderWeight = Math.Max(1, FormWeight);
        config.DailySendingLimit = int.TryParse(FormDailyLimit, out var dl) ? dl : null;
        config.SendsPerMinute = int.TryParse(FormSendsPerMinute, out var spm) ? spm : null;

        config.SmtpHost = FormSmtpHost;
        config.SmtpPort = FormSmtpPort;
        config.SmtpUsername = FormSmtpUsername;
        config.SmtpPassword = FormSmtpPassword;
        config.SmtpUseSsl = FormSmtpUseSsl;

        config.ResendApiKey = FormResendApiKey;

        config.AwsAccessKeyId = FormAwsAccessKeyId;
        config.AwsSecretAccessKey = FormAwsSecretAccessKey;
        config.AwsRegion = FormAwsRegion;

        config.SendGridApiKey = FormSendGridApiKey;

        config.MailgunApiKey = FormMailgunApiKey;
        config.MailgunDomain = FormMailgunDomain;

        config.PhysicalAddress = string.IsNullOrWhiteSpace(FormPhysicalAddress) ? null : FormPhysicalAddress;
        config.UnsubscribeBaseUrl = string.IsNullOrWhiteSpace(FormUnsubscribeBaseUrl) ? null : FormUnsubscribeBaseUrl;

        if (config.Id == 0)
            await _repo.AddAsync(config);
        else
            await _repo.UpdateAsync(config);

        await LoadAsync();
        IsEditing = false;
        StatusMessage = "Provider saved.";
    }

    [RelayCommand]
    private async Task DeleteProviderAsync(EmailProviderConfig provider)
    {
        await _repo.DeleteAsync(provider.Id);
        await LoadAsync();
        StatusMessage = "Provider deleted.";
    }

    [RelayCommand]
    private async Task SetDefaultAsync(EmailProviderConfig provider)
    {
        await _repo.SetDefaultAsync(provider.Id);
        await LoadAsync();
        StatusMessage = $"{provider.Name} set as default.";
    }

    [RelayCommand]
    private async Task TestConnectionAsync(EmailProviderConfig provider)
    {
        StatusMessage = "Testing connection...";
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var ok = await _emailService.TestConnectionAsync(provider.Id, cts.Token);
            StatusMessage = ok ? "Connection successful!" : "Connection failed. Check your settings.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Connection timed out. Check host/port and try again.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ClearForm();
    }

    private void ClearForm()
    {
        FormName = FormSenderName = FormSenderEmail = FormSmtpHost =
        FormSmtpUsername = FormSmtpPassword = FormResendApiKey =
        FormAwsAccessKeyId = FormAwsSecretAccessKey = FormAwsRegion =
        FormSendGridApiKey = FormMailgunApiKey = FormMailgunDomain =
        FormPhysicalAddress = FormUnsubscribeBaseUrl =
        FormDailyLimit = FormSendsPerMinute = "";
        FormAwsRegion = "us-east-1";
        FormSmtpPort = 587;
        FormSmtpUseSsl = true;
        FormIsEnabled = true;
        FormWeight = 1;
        FormType = ProviderType.Smtp;
    }
}
