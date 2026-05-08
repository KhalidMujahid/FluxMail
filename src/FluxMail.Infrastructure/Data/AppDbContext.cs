using FluxMail.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FluxMail.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<EmailProviderConfig> Providers => Set<EmailProviderConfig>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactList> ContactLists => Set<ContactList>();
    public DbSet<ContactListMembership> ContactListMemberships => Set<ContactListMembership>();
    public DbSet<EmailTemplate> Templates => Set<EmailTemplate>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<EmailQueueItem> Queue => Set<EmailQueueItem>();
    public DbSet<EmailTrackingEvent> TrackingEvents => Set<EmailTrackingEvent>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<ContactListMembership>()
            .HasKey(m => new { m.ContactId, m.ContactListId });

        model.Entity<ContactListMembership>()
            .HasOne(m => m.Contact).WithMany(c => c.ListMemberships).HasForeignKey(m => m.ContactId);

        model.Entity<ContactListMembership>()
            .HasOne(m => m.ContactList).WithMany(cl => cl.Memberships).HasForeignKey(m => m.ContactListId);

        model.Entity<Campaign>()
            .HasOne(c => c.ContactList).WithMany().HasForeignKey(c => c.ContactListId).OnDelete(DeleteBehavior.SetNull);

        model.Entity<Campaign>()
            .HasOne(c => c.Provider).WithMany().HasForeignKey(c => c.ProviderId).OnDelete(DeleteBehavior.SetNull);

        model.Entity<EmailLog>()
            .HasOne(l => l.Campaign).WithMany(c => c.Logs).HasForeignKey(l => l.CampaignId).OnDelete(DeleteBehavior.SetNull);

        model.Entity<EmailProviderConfig>().Property(p => p.Type).HasConversion<string>();
        model.Entity<Campaign>().Property(c => c.Status).HasConversion<string>();
        model.Entity<Campaign>().Property(c => c.Recurrence).HasConversion<string>();
        model.Entity<EmailLog>().Property(l => l.Status).HasConversion<string>();
        model.Entity<EmailQueueItem>().Property(q => q.Status).HasConversion<string>();
        model.Entity<EmailQueueItem>().Property(q => q.Priority).HasConversion<string>();
        model.Entity<EmailTrackingEvent>().Property(t => t.EventType).HasConversion<string>();

        model.Entity<EmailQueueItem>()
            .HasIndex(q => new { q.Status, q.Priority, q.ScheduledAt });
    }
}
