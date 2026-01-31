namespace WeeklyHabitTracker.Services;

/// <summary>Simple in-memory rate limiter for homepage requests.</summary>
public class RateLimitService
{
    private readonly Dictionary<string, (int Count, DateTime WindowStart)> _store = new();
    private readonly int _limit = 120;
    private readonly TimeSpan _window = TimeSpan.FromMinutes(1);
    private readonly object _lock = new();

    public bool TryAcquire(string key)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (_store.TryGetValue(key, out var entry))
            {
                if (now - entry.WindowStart >= _window)
                {
                    _store[key] = (1, now);
                    return true;
                }
                if (entry.Count >= _limit)
                    return false;
                _store[key] = (entry.Count + 1, entry.WindowStart);
                return true;
            }
            _store[key] = (1, now);
            return true;
        }
    }
}
