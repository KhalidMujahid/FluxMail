using System.Collections.Concurrent;

namespace FluxMail.Core.Services;

public class ProviderRateLimiter
{
    private readonly ConcurrentDictionary<int, RateWindow> _windows = new();

    public bool TryAcquire(int providerId, int? maxPerMinute)
    {
        if (!maxPerMinute.HasValue || maxPerMinute.Value <= 0) return true;

        var window = _windows.GetOrAdd(providerId, _ => new RateWindow());
        lock (window)
        {
            var now = DateTime.UtcNow;
            if ((now - window.WindowStart).TotalMinutes >= 1)
            {
                window.Count = 0;
                window.WindowStart = now;
            }
            if (window.Count >= maxPerMinute.Value) return false;
            window.Count++;
            return true;
        }
    }

    private sealed class RateWindow
    {
        public int Count;
        public DateTime WindowStart = DateTime.UtcNow;
    }
}
