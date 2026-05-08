using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;

namespace FluxMail.Desktop.ViewModels;

public partial class AnalyticsViewModel : ViewModelBase, IAsyncLoadable
{
    private readonly ICampaignRepository _campaignRepo;
    private readonly ITrackingRepository _trackingRepo;
    private readonly IEmailLogRepository _logRepo;
    private readonly IProviderRepository _providerRepo;

    [ObservableProperty] private ObservableCollection<CampaignAnalytics> _campaignStats = [];
    [ObservableProperty] private ObservableCollection<ProviderAnalytics> _providerStats = [];
    [ObservableProperty] private int _totalSent;
    [ObservableProperty] private int _totalFailed;
    [ObservableProperty] private int _totalOpens;
    [ObservableProperty] private int _totalClicks;
    [ObservableProperty] private bool _isLoading;

    public AnalyticsViewModel(
        ICampaignRepository campaignRepo,
        ITrackingRepository trackingRepo,
        IEmailLogRepository logRepo,
        IProviderRepository providerRepo)
    {
        _campaignRepo = campaignRepo;
        _trackingRepo = trackingRepo;
        _logRepo = logRepo;
        _providerRepo = providerRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;

        TotalSent = await _logRepo.GetTotalSentCountAsync();
        TotalFailed = await _logRepo.GetFailedTodayCountAsync();

        var campaigns = await _campaignRepo.GetAllAsync();
        var stats = new List<CampaignAnalytics>();

        foreach (var c in campaigns.Where(c => c.TotalRecipients > 0))
        {
            var (opens, clicks) = await _trackingRepo.GetCampaignStatsAsync(c.Id);
            stats.Add(new CampaignAnalytics
            {
                CampaignName = c.Name,
                Status = c.Status.ToString(),
                TotalRecipients = c.TotalRecipients,
                Sent = c.SentCount,
                Failed = c.FailedCount,
                Opens = opens,
                Clicks = clicks,
                DeliveryRate = c.TotalRecipients == 0 ? 0 : Math.Round((double)c.SentCount / c.TotalRecipients * 100, 1),
                OpenRate = c.SentCount == 0 ? 0 : Math.Round((double)opens / c.SentCount * 100, 1),
                ClickRate = c.SentCount == 0 ? 0 : Math.Round((double)clicks / c.SentCount * 100, 1)
            });
        }

        CampaignStats = new ObservableCollection<CampaignAnalytics>(stats);
        TotalOpens = stats.Sum(s => s.Opens);
        TotalClicks = stats.Sum(s => s.Clicks);

        var providers = await _providerRepo.GetAllAsync();
        var pStats = providers.Select(p => new ProviderAnalytics
        {
            Name = p.Name,
            Type = p.Type.ToString(),
            IsDefault = p.IsDefault,
            IsEnabled = p.IsEnabled,
            Weight = p.ProviderWeight,
            DailyLimit = p.DailySendingLimit?.ToString() ?? "∞"
        }).ToList();

        ProviderStats = new ObservableCollection<ProviderAnalytics>(pStats);

        IsLoading = false;
    }
}

public class CampaignAnalytics
{
    public string CampaignName { get; init; } = "";
    public string Status { get; init; } = "";
    public int TotalRecipients { get; init; }
    public int Sent { get; init; }
    public int Failed { get; init; }
    public int Opens { get; init; }
    public int Clicks { get; init; }
    public double DeliveryRate { get; init; }
    public double OpenRate { get; init; }
    public double ClickRate { get; init; }
}

public class ProviderAnalytics
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public bool IsDefault { get; init; }
    public bool IsEnabled { get; init; }
    public int Weight { get; init; }
    public string DailyLimit { get; init; } = "∞";
}
