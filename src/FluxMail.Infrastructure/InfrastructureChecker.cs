using System.Net;
using System.Net.Sockets;
using FluxMail.Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace FluxMail.Infrastructure;

public class InfrastructureChecker
{
    public async Task<SmtpCheckResult> CheckSmtpAsync(EmailProviderConfig config, CancellationToken ct = default)
    {
        var result = new SmtpCheckResult { ProviderName = config.Name };
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(config.SmtpHost ?? "", config.SmtpPort,
                config.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
            result.ConnectionOk = true;

            if (!string.IsNullOrEmpty(config.SmtpUsername))
            {
                await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword ?? "", ct);
                result.AuthOk = true;
            }

            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }
        return result;
    }

    public async Task<DnsCheckResult> CheckDnsAsync(string domain, CancellationToken ct = default)
    {
        var result = new DnsCheckResult { Domain = domain };
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(domain, AddressFamily.Unspecified, ct);
            result.HasMxRecord = addresses.Length > 0;
            result.ResolvedAddresses = addresses.Select(a => a.ToString()).ToList();
        }
        catch
        {
            result.HasMxRecord = false;
        }

        result.SpfCheckNote = "Add a TXT record: v=spf1 include:your-provider.com ~all";
        result.DkimCheckNote = "Add a TXT record at selector._domainkey." + domain + ": v=DKIM1; k=rsa; p=<your-public-key>";
        result.DmarcCheckNote = "Add a TXT record at _dmarc." + domain + ": v=DMARC1; p=quarantine; rua=mailto:dmarc@" + domain;
        return result;
    }
}

public class SmtpCheckResult
{
    public string ProviderName { get; init; } = "";
    public bool ConnectionOk { get; set; }
    public bool AuthOk { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DnsCheckResult
{
    public string Domain { get; init; } = "";
    public bool HasMxRecord { get; set; }
    public List<string> ResolvedAddresses { get; set; } = [];
    public string SpfCheckNote { get; set; } = "";
    public string DkimCheckNote { get; set; } = "";
    public string DmarcCheckNote { get; set; } = "";
}
