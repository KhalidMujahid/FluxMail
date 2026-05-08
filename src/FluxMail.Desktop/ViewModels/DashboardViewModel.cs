using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;

namespace FluxMail.Desktop.ViewModels;

public partial class DashboardViewModel : ViewModelBase, IAsyncLoadable
{
    private readonly IEmailLogRepository _logRepo;
    private readonly IProviderRepository _providerRepo;

    [ObservableProperty] private int _sentToday;
    [ObservableProperty] private int _failedToday;
    [ObservableProperty] private int _totalSent;
    [ObservableProperty] private int _providerCount;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<EmailLog> _recentActivity = [];

    public DashboardViewModel(IEmailLogRepository logRepo, IProviderRepository providerRepo)
    {
        _logRepo = logRepo;
        _providerRepo = providerRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            SentToday = await _logRepo.GetSentTodayCountAsync();
            FailedToday = await _logRepo.GetFailedTodayCountAsync();
            TotalSent = await _logRepo.GetTotalSentCountAsync();
            var providers = await _providerRepo.GetAllAsync();
            ProviderCount = providers.Count;

            var recent = await _logRepo.GetRecentAsync(20);
            RecentActivity = new ObservableCollection<EmailLog>(recent);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
