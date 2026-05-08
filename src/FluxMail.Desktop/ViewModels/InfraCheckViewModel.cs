using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Core.Services;
using FluxMail.Infrastructure;

namespace FluxMail.Desktop.ViewModels;

public partial class InfraCheckViewModel : ViewModelBase, IAsyncLoadable
{
    private readonly InfrastructureChecker _checker;
    private readonly EmailVerifier _verifier;
    private readonly SpamAnalyzer _spamAnalyzer;
    private readonly IProviderRepository _providerRepo;

    [ObservableProperty] private ObservableCollection<EmailProviderConfig> _providers = [];
    [ObservableProperty] private EmailProviderConfig? _selectedProvider;
    [ObservableProperty] private bool _isChecking;
    [ObservableProperty] private string _smtpStatus = "";
    [ObservableProperty] private bool _smtpConnected;
    [ObservableProperty] private bool _smtpAuthenticated;

    // DNS check
    [ObservableProperty] private string _dnsDomain = "";
    [ObservableProperty] private string _dnsResult = "";
    [ObservableProperty] private bool _hasMxRecord;
    [ObservableProperty] private string _spfNote = "";
    [ObservableProperty] private string _dkimNote = "";
    [ObservableProperty] private string _dmarcNote = "";

    // Email verifier
    [ObservableProperty] private string _verifyEmail = "";
    [ObservableProperty] private string _verifyResult = "";
    [ObservableProperty] private ObservableCollection<string> _verifyIssues = [];
    [ObservableProperty] private bool _isVerifyValid;

    // Spam analyzer
    [ObservableProperty] private string _spamSubject = "";
    [ObservableProperty] private string _spamBody = "";
    [ObservableProperty] private double _spamScore;
    [ObservableProperty] private string _spamRisk = "";
    [ObservableProperty] private ObservableCollection<string> _spamIssues = [];

    public InfraCheckViewModel(
        InfrastructureChecker checker,
        EmailVerifier verifier,
        SpamAnalyzer spamAnalyzer,
        IProviderRepository providerRepo)
    {
        _checker = checker;
        _verifier = verifier;
        _spamAnalyzer = spamAnalyzer;
        _providerRepo = providerRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var providers = await _providerRepo.GetAllAsync();
        Providers = new ObservableCollection<EmailProviderConfig>(
            providers.Where(p => p.Type == ProviderType.Smtp));
        SelectedProvider = Providers.FirstOrDefault();
    }

    [RelayCommand]
    private async Task CheckSmtpAsync()
    {
        if (SelectedProvider is null) return;
        IsChecking = true;
        SmtpStatus = "Checking...";

        var result = await _checker.CheckSmtpAsync(SelectedProvider);
        SmtpConnected = result.ConnectionOk;
        SmtpAuthenticated = result.AuthOk;
        SmtpStatus = result.ConnectionOk
            ? (result.AuthOk ? "Connected and authenticated." : "Connected (no auth checked).")
            : $"Failed: {result.ErrorMessage}";
        IsChecking = false;
    }

    [RelayCommand]
    private async Task CheckDnsAsync()
    {
        if (string.IsNullOrWhiteSpace(DnsDomain)) return;
        IsChecking = true;
        DnsResult = "Checking DNS...";

        var result = await _checker.CheckDnsAsync(DnsDomain);
        HasMxRecord = result.HasMxRecord;
        DnsResult = result.HasMxRecord
            ? $"Domain resolves ({string.Join(", ", result.ResolvedAddresses.Take(3))})"
            : "Domain could not be resolved.";
        SpfNote = result.SpfCheckNote;
        DkimNote = result.DkimCheckNote;
        DmarcNote = result.DmarcCheckNote;
        IsChecking = false;
    }

    [RelayCommand]
    private async Task VerifyEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(VerifyEmail)) return;
        IsChecking = true;

        var result = await _verifier.VerifyAsync(VerifyEmail);
        IsVerifyValid = result.IsValid;
        VerifyResult = result.IsValid ? "Valid email address." : "Email has issues — see below.";
        VerifyIssues = new ObservableCollection<string>(result.Issues);
        IsChecking = false;
    }

    [RelayCommand]
    private void AnalyzeSpamAsync()
    {
        if (string.IsNullOrWhiteSpace(SpamSubject) && string.IsNullOrWhiteSpace(SpamBody)) return;

        var result = _spamAnalyzer.Analyze(SpamSubject, SpamBody);
        SpamScore = result.Score;
        SpamRisk = result.Risk;
        SpamIssues = new ObservableCollection<string>(result.Issues);
    }
}
