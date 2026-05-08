using FluxMail.Core.Interfaces;
using FluxMail.Core.Services;
using FluxMail.Infrastructure.BackgroundServices;
using FluxMail.Infrastructure.Data;
using FluxMail.Infrastructure.Providers;
using FluxMail.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluxMail.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFluxMailInfrastructure(
        this IServiceCollection services,
        string dbPath,
        TrackingConfig? trackingConfig = null)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        // Repositories
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IEmailLogRepository, EmailLogRepository>();
        services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();
        services.AddScoped<ITrackingRepository, TrackingRepository>();

        // Email providers
        services.AddScoped<IEmailProvider, SmtpEmailProvider>();
        services.AddScoped<IEmailProvider, ResendEmailProvider>();
        services.AddScoped<IEmailProvider, SesEmailProvider>();
        services.AddScoped<IEmailProvider, SendGridEmailProvider>();
        services.AddScoped<IEmailProvider, MailgunEmailProvider>();

        // Singletons for shared state
        services.AddSingleton<RotationState>();
        services.AddSingleton<ProviderRateLimiter>();
        services.AddSingleton(trackingConfig ?? new TrackingConfig());

        // Core services
        services.AddScoped<ProviderRotationService>();
        services.AddScoped<EmailService>();
        services.AddScoped<CampaignService>();
        services.AddScoped<TemplateService>();
        services.AddScoped<SpamAnalyzer>();
        services.AddScoped<EmailVerifier>();
        services.AddScoped<Infrastructure.InfrastructureChecker>();
        services.AddScoped<TrackingService>();

        // Background services
        services.AddHostedService<BackgroundQueueProcessor>();
        services.AddHostedService<CampaignScheduler>();

        // HTTP clients
        services.AddHttpClient("resend");
        services.AddHttpClient("sendgrid");
        services.AddHttpClient("mailgun");
        services.AddHttpClient("ses");

        return services;
    }

    public static void EnsureDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DatabaseMigrator.Migrate(db);
    }
}
