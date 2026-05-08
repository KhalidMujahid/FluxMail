using FluxMail.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FluxMail.Infrastructure.Data;

public static class DatabaseMigrator
{
    public static void Migrate(AppDbContext db)
    {
        db.Database.EnsureCreated();

        var conn = (SqliteConnection)db.Database.GetDbConnection();
        conn.Open();

        // EmailProviderConfig new columns
        AddColumnSafe(conn, "Providers", "ProviderWeight", "INTEGER NOT NULL DEFAULT 1");
        AddColumnSafe(conn, "Providers", "DailySendingLimit", "INTEGER NULL");
        AddColumnSafe(conn, "Providers", "SendsPerMinute", "INTEGER NULL");
        AddColumnSafe(conn, "Providers", "IsEnabled", "INTEGER NOT NULL DEFAULT 1");
        AddColumnSafe(conn, "Providers", "AwsAccessKeyId", "TEXT NULL");
        AddColumnSafe(conn, "Providers", "AwsSecretAccessKey", "TEXT NULL");
        AddColumnSafe(conn, "Providers", "AwsRegion", "TEXT NULL DEFAULT 'us-east-1'");
        AddColumnSafe(conn, "Providers", "SendGridApiKey", "TEXT NULL");
        AddColumnSafe(conn, "Providers", "MailgunApiKey", "TEXT NULL");
        AddColumnSafe(conn, "Providers", "MailgunDomain", "TEXT NULL");

        // Campaign new columns
        AddColumnSafe(conn, "Campaigns", "ScheduledAt", "TEXT NULL");
        AddColumnSafe(conn, "Campaigns", "Recurrence", "TEXT NOT NULL DEFAULT 'None'");
        AddColumnSafe(conn, "Campaigns", "RecurrenceInterval", "INTEGER NOT NULL DEFAULT 1");
        AddColumnSafe(conn, "Campaigns", "NextRunAt", "TEXT NULL");
        AddColumnSafe(conn, "Campaigns", "FromNameOverride", "TEXT NULL");

        // EmailLog new columns
        AddColumnSafe(conn, "EmailLogs", "TrackingId", "TEXT NULL");
        AddColumnSafe(conn, "EmailLogs", "OpenCount", "INTEGER NOT NULL DEFAULT 0");
        AddColumnSafe(conn, "EmailLogs", "ClickCount", "INTEGER NOT NULL DEFAULT 0");

        // UserProfiles table — created by EnsureCreated on first run; ensure it exists for upgraded DBs
        EnsureTableSafe(conn, "UserProfiles",
            "\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"FullName\" TEXT NOT NULL DEFAULT '', " +
            "\"Email\" TEXT NOT NULL DEFAULT '', " +
            "\"PasswordHash\" TEXT NOT NULL DEFAULT '', " +
            "\"CreatedAt\" TEXT NOT NULL DEFAULT ''");

        conn.Close();
    }

    private static void AddColumnSafe(SqliteConnection conn, string table, string column, string definition)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition}";
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // Column already exists — safe to ignore
        }
    }

    private static void EnsureTableSafe(SqliteConnection conn, string table, string columnDefs)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE TABLE IF NOT EXISTS \"{table}\" ({columnDefs})";
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // Table already exists — safe to ignore
        }
    }
}
