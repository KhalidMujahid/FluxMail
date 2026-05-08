using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Desktop.Services;

namespace FluxMail.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboard;
    private readonly ProvidersViewModel _providers;
    private readonly ContactsViewModel _contacts;
    private readonly TemplatesViewModel _templates;
    private readonly ComposeViewModel _compose;
    private readonly CampaignsViewModel _campaigns;
    private readonly LogsViewModel _logs;
    private readonly AnalyticsViewModel _analytics;
    private readonly InfraCheckViewModel _infraCheck;
    private readonly SettingsViewModel _settings;

    private readonly HashSet<ViewModelBase> _loaded = [];

    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private string _activeNav = "Dashboard";

    public CurrentUserService CurrentUser { get; }
    public NotificationService Notifications { get; }
    public ObservableCollection<ToastItem> Toasts => Notifications.Items;

    public event EventHandler? LogoutRequested;

    public MainWindowViewModel(
        DashboardViewModel dashboard,
        ProvidersViewModel providers,
        ContactsViewModel contacts,
        TemplatesViewModel templates,
        ComposeViewModel compose,
        CampaignsViewModel campaigns,
        LogsViewModel logs,
        AnalyticsViewModel analytics,
        InfraCheckViewModel infraCheck,
        SettingsViewModel settings,
        CurrentUserService currentUser,
        NotificationService notifications)
    {
        _dashboard = dashboard;
        _providers = providers;
        _contacts = contacts;
        _templates = templates;
        _compose = compose;
        _campaigns = campaigns;
        _logs = logs;
        _analytics = analytics;
        _infraCheck = infraCheck;
        _settings = settings;
        CurrentUser = currentUser;
        Notifications = notifications;
        _currentPage = dashboard;
    }

    [RelayCommand] private void NavigateToDashboard() => Navigate("Dashboard", _dashboard);
    [RelayCommand] private void NavigateToProviders() => Navigate("Providers", _providers);
    [RelayCommand] private void NavigateToContacts() => Navigate("Contacts", _contacts);
    [RelayCommand] private void NavigateToTemplates() => Navigate("Templates", _templates);
    [RelayCommand] private void NavigateToCompose() => Navigate("Compose", _compose);
    [RelayCommand] private void NavigateToCampaigns() => Navigate("Campaigns", _campaigns);
    [RelayCommand] private void NavigateToLogs() => Navigate("Logs", _logs);
    [RelayCommand] private void NavigateToAnalytics() => Navigate("Analytics", _analytics);
    [RelayCommand] private void NavigateToInfraCheck() => Navigate("InfraCheck", _infraCheck);
    [RelayCommand] private void NavigateToSettings() => Navigate("Settings", _settings);

    [RelayCommand]
    private void Logout() => LogoutRequested?.Invoke(this, EventArgs.Empty);

    private void Navigate(string name, ViewModelBase vm)
    {
        ActiveNav = name;
        CurrentPage = vm;
        // Only call LoadAsync on first visit — preserves form state on return
        if (vm is IAsyncLoadable loadable && _loaded.Add(vm))
            _ = loadable.LoadAsync();
    }
}

public interface IAsyncLoadable
{
    Task LoadAsync();
}
