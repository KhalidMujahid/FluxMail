using System.Text.RegularExpressions;

namespace FluxMail.Core.Services;

public class SpamAnalyzer
{
    private static readonly string[] SpamPhrases =
    [
        "free", "winner", "congratulations", "prize", "click here", "act now",
        "limited time offer", "urgent", "buy now", "make money", "100% free",
        "guaranteed", "risk free", "call now", "order now", "special offer",
        "double your income", "earn extra cash", "no obligation", "you have won",
        "no credit check", "lose weight", "as seen on", "work from home",
        "increase your sales", "dear friend", "this is not spam"
    ];

    public SpamAnalysisResult Analyze(string subject, string htmlBody)
    {
        var issues = new List<string>();
        var score = 0.0;

        var subjectLower = subject.ToLowerInvariant();
        var bodyLower = htmlBody.ToLowerInvariant();

        foreach (var phrase in SpamPhrases)
        {
            if (subjectLower.Contains(phrase))
            {
                score += 1.5;
                issues.Add($"Spam phrase in subject: \"{phrase}\"");
            }
        }

        var bodyHits = SpamPhrases.Count(p => bodyLower.Contains(p));
        if (bodyHits > 0)
        {
            score += bodyHits * 0.4;
            if (bodyHits > 2) issues.Add($"{bodyHits} spam trigger phrases found in body");
        }

        var capsWords = subject.Split(' ').Count(w => w.Length > 3 && w == w.ToUpper() && w.Any(char.IsLetter));
        if (capsWords > 1)
        {
            score += capsWords * 0.7;
            issues.Add($"{capsWords} all-caps words in subject (avoid shouting)");
        }

        var exclamations = subject.Count(c => c == '!') + htmlBody.Count(c => c == '!');
        if (exclamations > 3)
        {
            score += Math.Min(exclamations * 0.3, 2.0);
            issues.Add($"Too many exclamation marks ({exclamations})");
        }

        if (!bodyLower.Contains("unsubscribe"))
        {
            score += 2.5;
            issues.Add("Missing unsubscribe link — required by CAN-SPAM / GDPR");
        }

        var imgCount = Regex.Matches(htmlBody, "<img", RegexOptions.IgnoreCase).Count;
        var textOnly = Regex.Replace(htmlBody, "<.*?>", " ").Trim();
        if (imgCount > 2 && textOnly.Length < 100)
        {
            score += 1.5;
            issues.Add("High image-to-text ratio — often flagged by spam filters");
        }

        if (subject.Length < 5)
        {
            score += 1.0;
            issues.Add("Subject line is too short");
        }

        if (subjectLower.StartsWith("re:") || subjectLower.StartsWith("fwd:"))
        {
            score += 2.0;
            issues.Add("Subject starts with Re:/Fwd: — common phishing pattern");
        }

        score = Math.Min(Math.Round(score, 1), 10.0);

        return new SpamAnalysisResult
        {
            Score = score,
            Risk = score < 2 ? "Low" : score < 5 ? "Medium" : "High",
            Issues = issues
        };
    }
}

public class SpamAnalysisResult
{
    public double Score { get; init; }
    public string Risk { get; init; } = "Low";
    public List<string> Issues { get; init; } = [];
}
