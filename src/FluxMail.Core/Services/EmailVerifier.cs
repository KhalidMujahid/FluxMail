using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace FluxMail.Core.Services;

public class EmailVerifier
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com", "tempmail.com", "guerrillamail.com", "throwaway.email",
        "yopmail.com", "10minutemail.com", "sharklasers.com", "trashmail.com",
        "maildrop.cc", "dispostable.com", "spamgourmet.com", "filzmail.com",
        "mytrashmail.com", "tempr.email", "fakeinbox.com", "spam4.me"
    };

    public async Task<EmailVerificationResult> VerifyAsync(string email, CancellationToken ct = default)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
        {
            issues.Add("Invalid email syntax");
            return new EmailVerificationResult { Email = email, IsValid = false, Issues = issues };
        }

        var parts = email.Split('@');
        var domain = parts[1].ToLowerInvariant();

        if (DisposableDomains.Contains(domain))
        {
            issues.Add("Disposable / temporary email domain detected");
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(domain, AddressFamily.Unspecified, ct);
            if (addresses.Length == 0)
                issues.Add("Domain has no DNS records");
        }
        catch (SocketException)
        {
            issues.Add("Domain could not be resolved — may not exist");
        }
        catch (OperationCanceledException) { }

        var isValid = !issues.Any(i => i.Contains("syntax") || i.Contains("resolved"));
        return new EmailVerificationResult { Email = email, IsValid = isValid, Issues = issues };
    }

    public async Task<List<EmailVerificationResult>> VerifyBatchAsync(
        IEnumerable<string> emails,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var results = new List<EmailVerificationResult>();
        var count = 0;

        foreach (var email in emails)
        {
            if (ct.IsCancellationRequested) break;
            results.Add(await VerifyAsync(email, ct));
            progress?.Report(++count);
        }

        return results;
    }
}

public class EmailVerificationResult
{
    public string Email { get; init; } = "";
    public bool IsValid { get; init; }
    public List<string> Issues { get; init; } = [];
}
