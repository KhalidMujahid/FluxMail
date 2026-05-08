using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Core.Services;
using FluxMail.Desktop.Services;
using FluxMail.Desktop.ViewModels;
using FluxMail.Desktop.Views;
using FluxMail.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluxMail.Desktop;

public partial class App : Application
{
    private IHost? _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FluxMail", "fluxmail.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));

                services.AddFluxMailInfrastructure(dbPath, new TrackingConfig
                {
                    Enabled = false,
                    TrackingDomain = ""
                });

                // Singletons shared across the Desktop layer
                services.AddSingleton<CurrentUserService>();
                services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();
                services.AddSingleton<NotificationService>();

                // Page ViewModels — transient (fresh state on each nav)
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ProvidersViewModel>();
                services.AddTransient<ContactsViewModel>();
                services.AddTransient<TemplatesViewModel>();
                services.AddTransient<ComposeViewModel>();
                services.AddTransient<CampaignsViewModel>();
                services.AddTransient<LogsViewModel>();
                services.AddTransient<AnalyticsViewModel>();
                services.AddTransient<InfraCheckViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Auth ViewModels — transient
                services.AddTransient<SetupViewModel>();
                services.AddTransient<LoginViewModel>();

                // Main window VM is singleton
                services.AddSingleton<MainWindowViewModel>(sp => new MainWindowViewModel(
                    sp.GetRequiredService<DashboardViewModel>(),
                    sp.GetRequiredService<ProvidersViewModel>(),
                    sp.GetRequiredService<ContactsViewModel>(),
                    sp.GetRequiredService<TemplatesViewModel>(),
                    sp.GetRequiredService<ComposeViewModel>(),
                    sp.GetRequiredService<CampaignsViewModel>(),
                    sp.GetRequiredService<LogsViewModel>(),
                    sp.GetRequiredService<AnalyticsViewModel>(),
                    sp.GetRequiredService<InfraCheckViewModel>(),
                    sp.GetRequiredService<SettingsViewModel>(),
                    sp.GetRequiredService<CurrentUserService>(),
                    sp.GetRequiredService<NotificationService>()
                ));
            })
            .Build();

        _host.Services.EnsureDatabase();
        _ = _host.StartAsync(CancellationToken.None);

        // Synchronous profile check — lightweight DB query on startup
        UserProfile? existingProfile;
        using (var scope = _host.Services.CreateScope())
        {
            existingProfile = scope.ServiceProvider
                .GetRequiredService<IUserProfileRepository>()
                .GetAsync().GetAwaiter().GetResult();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (existingProfile is null)
                ShowSetupWindow(desktop);
            else
                ShowLoginWindow(desktop);

            desktop.ShutdownRequested += (_, _) => _host.StopAsync().Wait(TimeSpan.FromSeconds(5));
        }

        base.OnFrameworkInitializationCompleted();
    }

    // ── Setup (first launch) ──────────────────────────────────────────────

    private void ShowSetupWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var vm = _host!.Services.GetRequiredService<SetupViewModel>();
        var window = new SetupWindow { DataContext = vm };
        desktop.MainWindow = window;
        window.Show();

        vm.AccountCreated += (_, profile) =>
        {
            _host.Services.GetRequiredService<CurrentUserService>().Profile = profile;
            Dispatcher.UIThread.Post(() => OpenMainWindow(desktop, window));
        };
    }

    // ── Login (subsequent launches) ───────────────────────────────────────

    private void ShowLoginWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var vm = _host!.Services.GetRequiredService<LoginViewModel>();
        var window = new LoginWindow { DataContext = vm };
        desktop.MainWindow = window;
        window.Show();

        vm.LoginSuccessful += (_, profile) =>
        {
            _host.Services.GetRequiredService<CurrentUserService>().Profile = profile;
            Dispatcher.UIThread.Post(() => OpenMainWindow(desktop, window));
        };
    }

    // ── Main window ───────────────────────────────────────────────────────

    private void OpenMainWindow(IClassicDesktopStyleApplicationLifetime desktop, Window previous)
    {
        var mainVm = _host!.Services.GetRequiredService<MainWindowViewModel>();
        var mainWindow = new MainWindow { DataContext = mainVm };
        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        previous.Close();

        // Load dashboard on first open
        _ = mainVm.CurrentPage is DashboardViewModel dash
            ? dash.LoadAsync()
            : Task.CompletedTask;

        // Handle logout — re-show login window, do NOT close the app
        mainVm.LogoutRequested += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _host.Services.GetRequiredService<CurrentUserService>().Profile = null;
                // Open a fresh login window first, then close main
                var loginVm = _host.Services.GetRequiredService<LoginViewModel>();
                var loginWindow = new LoginWindow { DataContext = loginVm };
                loginWindow.Show();
                desktop.MainWindow = loginWindow;

                loginVm.LoginSuccessful += (_, profile) =>
                {
                    _host.Services.GetRequiredService<CurrentUserService>().Profile = profile;
                    Dispatcher.UIThread.Post(() => OpenMainWindow(desktop, loginWindow));
                };

                mainWindow.Close();
            });
        };
    }
}
