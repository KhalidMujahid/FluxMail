using System.Text.RegularExpressions;

namespace FluxMail.Core.Services;

public class TrackingService(TrackingConfig config)
{
    private static readonly Regex LinkRegex = new(
        @"href=""(https?://[^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string GenerateTrackingId() => Guid.NewGuid().ToString("N");

    public string InjectTrackingPixel(string html, string trackingId)
    {
        if (!config.Enabled || string.IsNullOrEmpty(config.TrackingDomain))
            return html;

        var pixel = $"<img src=\"{config.TrackingDomain}/track/open/{trackingId}\" " +
                    "width=\"1\" height=\"1\" border=\"0\" alt=\"\" style=\"display:none!important\" />";

        return html.Contains("</body>", StringComparison.OrdinalIgnoreCase)
            ? html.Replace("</body>", pixel + "</body>", StringComparison.OrdinalIgnoreCase)
            : html + pixel;
    }

    public string WrapLinks(string html, string trackingId)
    {
        if (!config.Enabled || string.IsNullOrEmpty(config.TrackingDomain))
            return html;

        return LinkRegex.Replace(html, m =>
        {
            var url = m.Groups[1].Value;
            if (url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("unsubscribe", StringComparison.OrdinalIgnoreCase))
                return m.Value;

            var encoded = Uri.EscapeDataString(url);
            return $"href=\"{config.TrackingDomain}/track/click/{trackingId}/{encoded}\"";
        });
    }

    public string PrepareForSending(string html, string trackingId)
    {
        var result = InjectTrackingPixel(html, trackingId);
        return WrapLinks(result, trackingId);
    }
}

public class TrackingConfig
{
    public bool Enabled { get; set; }
    public string TrackingDomain { get; set; } = "";
}
