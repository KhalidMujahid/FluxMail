using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;

namespace FluxMail.Desktop.ViewModels;

public partial class LogsViewModel : ViewModelBase, IAsyncLoadable
{
    private readonly IEmailLogRepository _logRepo;
    private readonly IEmailQueueRepository _queueRepo;

    [ObservableProperty] private ObservableCollection<EmailLog> _logs = [];
    [ObservableProperty] private ObservableCollection<EmailQueueItem> _queueItems = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private int _pendingQueueCount;
    [ObservableProperty] private int _deadQueueCount;
    [ObservableProperty] private string _statusMessage = "";

    public LogsViewModel(IEmailLogRepository logRepo, IEmailQueueRepository queueRepo)
    {
        _logRepo = logRepo;
        _queueRepo = queueRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        var logs = await _logRepo.GetRecentAsync(100);
        Logs = new ObservableCollection<EmailLog>(string.IsNullOrWhiteSpace(FilterText)
            ? logs
            : logs.Where(l => l.ToEmail.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                               l.Subject.Contains(FilterText, StringComparison.OrdinalIgnoreCase)));

        var queue = await _queueRepo.GetAllAsync(100);
        QueueItems = new ObservableCollection<EmailQueueItem>(queue);
        PendingQueueCount = await _queueRepo.GetPendingCountAsync();
        DeadQueueCount = await _queueRepo.GetDeadCountAsync();
        IsLoading = false;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task PurgeDeadAsync()
    {
        await _queueRepo.PurgeDeadAsync();
        StatusMessage = "Dead queue purged.";
        await LoadAsync();
    }

    partial void OnFilterTextChanged(string value) => _ = LoadAsync();
}
