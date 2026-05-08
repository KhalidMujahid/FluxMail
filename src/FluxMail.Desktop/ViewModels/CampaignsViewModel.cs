using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Core.Services;

namespace FluxMail.Desktop.ViewModels;

public partial class CampaignsViewModel : ViewModelBase, IAsyncLoadable
{
    private readonly CampaignService _campaignService;
    private readonly ICampaignRepository _campaignRepo;
    private readonly IContactRepository _contactRepo;
    private readonly IProviderRepository _providerRepo;

    [ObservableProperty] private ObservableCollection<Campaign> _campaigns = [];
    [ObservableProperty] private ObservableCollection<ContactList> _contactLists = [];
    [ObservableProperty] private ObservableCollection<EmailProviderConfig> _providers = [];
    [ObservableProperty] private Campaign? _selectedCampaign;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSending;

    // Form — content
    [ObservableProperty] private string _formName = "";
    [ObservableProperty] private string _formSubject = "";
    [ObservableProperty] private string _formHtmlBody = "";
    [ObservableProperty] private string _formPlainText = "";
    [ObservableProperty] private string _formFromName = "";
    [ObservableProperty] private ContactList? _formContactList;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormProviderSenderName))]
    private EmailProviderConfig? _formProvider;

    public string FormProviderSenderName => FormProvider?.SenderName ?? "";

    // Form — scheduling
    [ObservableProperty] private bool _formScheduleEnabled;
    [ObservableProperty] private DateTimeOffset? _formScheduleDate;
    [ObservableProperty] private TimeSpan? _formScheduleTime;
    [ObservableProperty] private RecurrenceType _formRecurrence = RecurrenceType.None;
    [ObservableProperty] private int _formRecurrenceInterval = 1;

    public RecurrenceType[] RecurrenceTypes { get; } = Enum.GetValues<RecurrenceType>();
    public bool IsRecurring => FormRecurrence != RecurrenceType.None;

    partial void OnFormRecurrenceChanged(RecurrenceType value)
        => OnPropertyChanged(nameof(IsRecurring));

    private CancellationTokenSource? _cts;

    public CampaignsViewModel(
        CampaignService campaignService,
        ICampaignRepository campaignRepo,
        IContactRepository contactRepo,
        IProviderRepository providerRepo)
    {
        _campaignService = campaignService;
        _campaignRepo = campaignRepo;
        _contactRepo = contactRepo;
        _providerRepo = providerRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        var campaigns = await _campaignRepo.GetAllAsync();
        Campaigns = new ObservableCollection<Campaign>(campaigns);

        var lists = await _contactRepo.GetAllListsAsync();
        ContactLists = new ObservableCollection<ContactList>(lists);

        var providers = await _providerRepo.GetAllAsync();
        Providers = new ObservableCollection<EmailProviderConfig>(providers);
        IsLoading = false;
    }

    [RelayCommand]
    private void NewCampaign()
    {
        SelectedCampaign = null;
        ClearForm();
        FormProvider = Providers.FirstOrDefault(p => p.IsDefault) ?? Providers.FirstOrDefault();
        IsEditing = true;
    }

    [RelayCommand]
    private void EditCampaign(Campaign campaign)
    {
        if (campaign.Status == CampaignStatus.Sending) return;
        SelectedCampaign = campaign;
        FormName = campaign.Name;
        FormSubject = campaign.Subject;
        FormHtmlBody = campaign.HtmlBody;
        FormPlainText = campaign.PlainTextBody ?? "";
        FormFromName = campaign.FromNameOverride ?? "";
        FormContactList = ContactLists.FirstOrDefault(l => l.Id == campaign.ContactListId);
        FormProvider = Providers.FirstOrDefault(p => p.Id == campaign.ProviderId);

        FormScheduleEnabled = campaign.ScheduledAt.HasValue;
        if (campaign.ScheduledAt.HasValue)
        {
            FormScheduleDate = new DateTimeOffset(campaign.ScheduledAt.Value, TimeSpan.Zero);
            FormScheduleTime = campaign.ScheduledAt.Value.TimeOfDay;
        }
        else
        {
            FormScheduleDate = null;
            FormScheduleTime = null;
        }

        FormRecurrence = campaign.Recurrence;
        FormRecurrenceInterval = campaign.RecurrenceInterval;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveCampaignAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName) || string.IsNullOrWhiteSpace(FormSubject))
        {
            StatusMessage = "Name and subject are required.";
            return;
        }

        var campaign = SelectedCampaign ?? new Campaign();
        campaign.Name = FormName;
        campaign.Subject = FormSubject;
        campaign.HtmlBody = FormHtmlBody;
        campaign.PlainTextBody = string.IsNullOrWhiteSpace(FormPlainText) ? null : FormPlainText;
        campaign.FromNameOverride = string.IsNullOrWhiteSpace(FormFromName) ? null : FormFromName;
        campaign.ContactListId = FormContactList?.Id;
        campaign.ProviderId = FormProvider?.Id;

        if (FormScheduleEnabled && FormScheduleDate.HasValue)
        {
            var date = FormScheduleDate.Value.Date;
            var time = FormScheduleTime ?? TimeSpan.Zero;
            var scheduled = DateTime.SpecifyKind(date + time, DateTimeKind.Utc);
            campaign.ScheduledAt = scheduled;
            campaign.NextRunAt = scheduled;
            campaign.Status = CampaignStatus.Scheduled;
        }
        else
        {
            campaign.ScheduledAt = null;
            campaign.NextRunAt = null;
            if (campaign.Status == CampaignStatus.Scheduled)
                campaign.Status = CampaignStatus.Draft;
        }

        campaign.Recurrence = FormRecurrence;
        campaign.RecurrenceInterval = Math.Max(1, FormRecurrenceInterval);

        if (campaign.Id == 0)
            await _campaignRepo.AddAsync(campaign);
        else
            await _campaignRepo.UpdateAsync(campaign);

        await LoadAsync();
        IsEditing = false;
        StatusMessage = FormScheduleEnabled ? "Campaign scheduled." : "Campaign saved.";
    }

    [RelayCommand]
    private async Task StartCampaignAsync(Campaign campaign)
    {
        if (campaign.Status == CampaignStatus.Sending) return;

        IsSending = true;
        StatusMessage = $"Sending '{campaign.Name}'...";
        _cts = new CancellationTokenSource();

        try
        {
            await _campaignService.StartAsync(campaign.Id, _cts.Token);
            await LoadAsync();
            StatusMessage = "Campaign completed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Campaign failed: {ex.Message}";
        }
        finally
        {
            IsSending = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void StopCampaign()
    {
        _cts?.Cancel();
        StatusMessage = "Stopping campaign...";
    }

    [RelayCommand]
    private async Task DeleteCampaignAsync(Campaign campaign)
    {
        if (campaign.Status == CampaignStatus.Sending) return;
        await _campaignRepo.DeleteAsync(campaign.Id);
        await LoadAsync();
        StatusMessage = "Campaign deleted.";
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ClearForm();
    }

    private void ClearForm()
    {
        FormName = FormSubject = FormHtmlBody = FormPlainText = FormFromName = "";
        FormContactList = null;
        FormProvider = null;
        FormScheduleEnabled = false;
        FormScheduleDate = null;
        FormScheduleTime = null;
        FormRecurrence = RecurrenceType.None;
        FormRecurrenceInterval = 1;
    }
}
