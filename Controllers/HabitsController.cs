using Microsoft.AspNetCore.Mvc;
using WeeklyHabitTracker.Models;
using WeeklyHabitTracker.Services;

namespace WeeklyHabitTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("api")]
public class HabitsController : ControllerBase
{
    private readonly HabitStorage _storage;

    public HabitsController(HabitStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Get all habits
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<Habit>> GetHabits()
    {
        return Ok(_storage.Habits);
    }

    /// <summary>
    /// Create a new habit
    /// </summary>
    [HttpPost]
    public ActionResult<Habit> CreateHabit([FromBody] CreateHabitRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Habit name is required.");
        var habit = new Habit
        {
            Id = _storage.NextHabitId(),
            Name = request.Name
        };
        _storage.AddHabit(habit);
        return CreatedAtAction(nameof(GetHabits), habit);
    }

    /// <summary>
    /// Log whether you did or didn't do a habit on a specific date (ISO: yyyy-MM-dd)
    /// </summary>
    [HttpPost("entries")]
    public ActionResult<HabitEntry> LogEntry([FromBody] LogEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Date))
            return BadRequest("Date is required.");
        var existing = _storage.Entries.FirstOrDefault(e => e.HabitId == request.HabitId && e.Date == request.Date);
        if (existing != null)
        {
            existing.Completed = request.Completed;
            _storage.AddOrUpdateEntry(existing);
            return Ok(existing);
        }
        var entry = new HabitEntry
        {
            Id = _storage.NextEntryId(),
            HabitId = request.HabitId,
            Date = request.Date,
            Completed = request.Completed
        };
        _storage.AddOrUpdateEntry(entry);
        return Ok(entry);
    }

    /// <summary>
    /// Get graph data. mode=weekly (default) or monthly. weekStart/monthStart in ISO yyyy-MM-dd
    /// </summary>
    [HttpGet("graph")]
    public ActionResult<IEnumerable<GraphDataPoint>> GetGraphData(
        [FromQuery] string? weekStart = null,
        [FromQuery] string? monthStart = null,
        [FromQuery] string? mode = "weekly")
    {
        if (mode == "monthly")
            return Ok(GetMonthlyGraphData(monthStart));
        return Ok(GetWeeklyGraphData(weekStart));
    }

    private List<GraphDataPoint> GetWeeklyGraphData(string? weekStart)
    {
        var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        var result = new List<GraphDataPoint>();
        DateTime start;
        if (string.IsNullOrEmpty(weekStart) || !DateTime.TryParseExact(weekStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out start))
            start = GetWeekStart(DateTime.Today);
        else
            start = GetWeekStart(start);
        for (int i = 0; i < 7; i++)
        {
            var d = start.AddDays(i);
            var dateStr = d.ToString("yyyy-MM-dd");
            var dayEntries = _storage.Entries.Where(e => e.Date == dateStr).ToList();
            var completed = dayEntries.Count(e => e.Completed);
            var total = _storage.Habits.Count;
            result.Add(new GraphDataPoint { Day = dayNames[i], CompletedCount = completed, TotalHabits = total > 0 ? total : 1 });
        }
        return result;
    }

    private List<GraphDataPoint> GetMonthlyGraphData(string? monthStart)
    {
        var result = new List<GraphDataPoint>();
        DateTime month;
        if (string.IsNullOrEmpty(monthStart) || !DateTime.TryParseExact(monthStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out month))
            month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        else
            month = new DateTime(month.Year, month.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
        for (int day = 1; day <= daysInMonth; day++)
        {
            var d = month.AddDays(day - 1);
            var dateStr = d.ToString("yyyy-MM-dd");
            var dayEntries = _storage.Entries.Where(e => e.Date == dateStr).ToList();
            var completed = dayEntries.Count(e => e.Completed);
            var total = _storage.Habits.Count;
            result.Add(new GraphDataPoint
            {
                Day = day.ToString(),
                CompletedCount = completed,
                TotalHabits = total > 0 ? total : 1
            });
        }
        return result;
    }

    /// <summary>
    /// Get awards for a month (monthStart in ISO yyyy-MM-dd, first day of month)
    /// </summary>
    [HttpGet("awards")]
    public ActionResult<AwardsResponse> GetAwards([FromQuery] string? monthStart = null)
    {
        if (_storage.Habits == null || _storage.Habits.Count == 0)
            return Ok(new AwardsResponse { TopActivity = null, LowestActivity = null, HighestStreakActivity = null });
        DateTime month;
        if (string.IsNullOrEmpty(monthStart) || !DateTime.TryParseExact(monthStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out month))
            month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        else
            month = new DateTime(month.Year, month.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
        var fromStr = month.ToString("yyyy-MM-dd");
        var toStr = month.AddDays(daysInMonth - 1).ToString("yyyy-MM-dd");
        var monthEntries = (_storage.Entries ?? new List<HabitEntry>()).Where(e => string.Compare(e.Date, fromStr, StringComparison.Ordinal) >= 0 && string.Compare(e.Date, toStr, StringComparison.Ordinal) <= 0).ToList();
        var completedByHabit = _storage.Habits.ToDictionary(h => h.Id, h => monthEntries.Count(e => e.HabitId == h.Id && e.Completed));
        if (completedByHabit.Count == 0)
            return Ok(new AwardsResponse { TopActivity = null, LowestActivity = null, HighestStreakActivity = null });
        var top = completedByHabit.OrderByDescending(x => x.Value).First();
        var lowest = completedByHabit.OrderBy(x => x.Value).First();
        var habitIds = _storage.Habits.Select(h => h.Id).ToList();
        var bestStreak = 0;
        int? bestStreakHabitId = null;
        foreach (var hid in habitIds)
        {
            var completedDates = monthEntries.Where(e => e.HabitId == hid && e.Completed).Select(e => e.Date).OrderBy(d => d).ToList();
            var streak = GetLongestStreak(completedDates);
            if (streak > bestStreak) { bestStreak = streak; bestStreakHabitId = hid; }
        }
        var topHabit = _storage.Habits.FirstOrDefault(h => h.Id == top.Key);
        var lowestHabit = _storage.Habits.FirstOrDefault(h => h.Id == lowest.Key);
        var streakHabit = bestStreakHabitId.HasValue ? _storage.Habits.FirstOrDefault(h => h.Id == bestStreakHabitId) : null;
        return Ok(new AwardsResponse
        {
            TopActivity = topHabit != null ? new AwardItem { Name = topHabit.Name, Count = top.Value } : null,
            LowestActivity = lowestHabit != null ? new AwardItem { Name = lowestHabit.Name, Count = lowest.Value } : null,
            HighestStreakActivity = streakHabit != null ? new AwardItem { Name = streakHabit.Name, Count = bestStreak } : null
        });
    }

    private static int GetLongestStreak(List<string> sortedDates)
    {
        if (sortedDates.Count == 0) return 0;
        var maxStreak = 1;
        var current = 1;
        for (int i = 1; i < sortedDates.Count; i++)
        {
            if (DateTime.TryParseExact(sortedDates[i], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d2) &&
                DateTime.TryParseExact(sortedDates[i - 1], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d1) &&
                (d2 - d1).TotalDays == 1)
                current++;
            else
                current = 1;
            if (current > maxStreak) maxStreak = current;
        }
        return maxStreak;
    }

    /// <summary>
    /// Get entries for a habit within a date range (fromDate, toDate in ISO yyyy-MM-dd)
    /// </summary>
    [HttpGet("{habitId}/entries")]
    public ActionResult<IEnumerable<HabitEntry>> GetEntries(int habitId, [FromQuery] string? fromDate = null, [FromQuery] string? toDate = null)
    {
        var list = _storage.Entries.Where(e => e.HabitId == habitId).AsEnumerable();
        if (!string.IsNullOrEmpty(fromDate))
            list = list.Where(e => string.Compare(e.Date, fromDate, StringComparison.Ordinal) >= 0);
        if (!string.IsNullOrEmpty(toDate))
            list = list.Where(e => string.Compare(e.Date, toDate, StringComparison.Ordinal) <= 0);
        return Ok(list);
    }

    private static DateTime GetWeekStart(DateTime d) => d.AddDays(-(int)d.DayOfWeek);
}

public class CreateHabitRequest
{
    public string Name { get; set; } = string.Empty;
}

public class LogEntryRequest
{
    public int HabitId { get; set; }
    public string Date { get; set; } = string.Empty;  // ISO yyyy-MM-dd
    public bool Completed { get; set; }
}

public class AwardsResponse
{
    public AwardItem? TopActivity { get; set; }
    public AwardItem? LowestActivity { get; set; }
    public AwardItem? HighestStreakActivity { get; set; }
}

public class AwardItem
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
