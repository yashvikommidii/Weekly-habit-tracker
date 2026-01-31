using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;
using System.Text.Json;
using WeeklyHabitTracker.Models;
using WeeklyHabitTracker.Services;

namespace WeeklyHabitTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly HabitStorage _storage;

    public ChatController(IConfiguration config, IHttpClientFactory httpFactory, HabitStorage storage)
    {
        _config = config;
        _http = httpFactory.CreateClient();
        _storage = storage;
    }

    private static DateTime GetWeekStart(DateTime d) => d.AddDays(-(int)d.DayOfWeek);

    private static int GetLongestStreak(List<string> sortedDates)
    {
        if (sortedDates.Count == 0) return 0;
        var maxStreak = 1;
        var current = 1;
        for (var i = 1; i < sortedDates.Count; i++)
        {
            if (DateTime.TryParseExact(sortedDates[i], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2) &&
                DateTime.TryParseExact(sortedDates[i - 1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1) &&
                (d2 - d1).TotalDays == 1)
                current++;
            else
                current = 1;
            if (current > maxStreak) maxStreak = current;
        }
        return maxStreak;
    }

    private string BuildUserDataContext()
    {
        var habits = _storage.Habits ?? new List<Habit>();
        var entries = _storage.Entries ?? new List<HabitEntry>();
        if (habits.Count == 0)
            return "The user has no habits yet. They haven't added any habits to track.";

        var sb = new StringBuilder();
        sb.AppendLine("The user's habit-tracking data:");
        sb.AppendLine();
        sb.AppendLine($"Habits ({habits.Count}):");
        foreach (var h in habits)
            sb.AppendLine($"  - {h.Name} (id={h.Id})");
        sb.AppendLine();

        var today = DateTime.Today;
        var weekStart = GetWeekStart(today);
        var weekEnd = weekStart.AddDays(6);
        var fromWeek = weekStart.ToString("yyyy-MM-dd");
        var toWeek = weekEnd.ToString("yyyy-MM-dd");
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var monthEnd = monthStart.AddDays(daysInMonth - 1);
        var fromMonth = monthStart.ToString("yyyy-MM-dd");
        var toMonth = monthEnd.ToString("yyyy-MM-dd");

        sb.AppendLine($"This week ({fromWeek} to {toWeek}):");
        foreach (var h in habits)
        {
            var weekEntries = entries.Where(e => e.HabitId == h.Id && string.Compare(e.Date, fromWeek, StringComparison.Ordinal) >= 0 && string.Compare(e.Date, toWeek, StringComparison.Ordinal) <= 0).ToList();
            var completed = weekEntries.Count(e => e.Completed);
            sb.AppendLine($"  - {h.Name}: {completed}/7 days completed");
        }
        sb.AppendLine();

        sb.AppendLine($"This month ({fromMonth} to {toMonth}):");
        var monthEntries = entries.Where(e => string.Compare(e.Date, fromMonth, StringComparison.Ordinal) >= 0 && string.Compare(e.Date, toMonth, StringComparison.Ordinal) <= 0).ToList();
        foreach (var h in habits)
        {
            var completed = monthEntries.Count(e => e.HabitId == h.Id && e.Completed);
            sb.AppendLine($"  - {h.Name}: {completed}/{daysInMonth} days completed");
        }
        sb.AppendLine();

        var completedByHabit = habits.ToDictionary(h => h.Id, h => monthEntries.Count(e => e.HabitId == h.Id && e.Completed));
        if (completedByHabit.Count > 0)
        {
            var top = completedByHabit.OrderByDescending(x => x.Value).First();
            var lowest = completedByHabit.OrderBy(x => x.Value).First();
            var topHabit = habits.FirstOrDefault(hl => hl.Id == top.Key);
            var lowHabit = habits.FirstOrDefault(hl => hl.Id == lowest.Key);

            sb.AppendLine("Awards of the month:");
            if (topHabit != null)
                sb.AppendLine($"  - Top performer: {topHabit.Name} ({top.Value} completions)");
            if (lowHabit != null && lowHabit.Id != topHabit?.Id)
                sb.AppendLine($"  - Needs attention: {lowHabit.Name} ({lowest.Value} completions)");

            var bestStreak = 0;
            string? streakHabitName = null;
            foreach (var h in habits)
            {
                var completedDates = monthEntries.Where(e => e.HabitId == h.Id && e.Completed).Select(e => e.Date).OrderBy(d => d).ToList();
                var streak = GetLongestStreak(completedDates);
                if (streak > bestStreak) { bestStreak = streak; streakHabitName = h.Name; }
            }
            if (streakHabitName != null)
                sb.AppendLine($"  - Longest streak: {streakHabitName} ({bestStreak} consecutive days)");
        }

        return sb.ToString();
    }

    /// <summary>Send a message to the habit-tracking assistant</summary>
    [HttpPost]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("chat")]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request)
    {
        var apiKey = _config["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            return StatusCode(500, new ChatResponse { Reply = "The assistant is temporarily unavailable. Please try again later." });

        var userData = BuildUserDataContext();
        var systemContent = @"You are a helpful habit-tracking assistant. The user's habit data is prepended to their message. Use it to answer. Never ask them to share data—you already have it. Give specific answers with their habit names and numbers.";

        var userMessageWithData = $"[Your habit data - use this to answer:]\n{userData}\n\n[User question:] {request.Message ?? ""}";

        var messages = new List<object>
        {
            new { role = "system", content = systemContent }
        };

        if (request.History != null)
        {
            foreach (var m in request.History.TakeLast(10))
                messages.Add(new { role = m.Role ?? "user", content = m.Content ?? "" });
        }
        messages.Add(new { role = "user", content = userMessageWithData });

        var body = new
        {
            model = "gpt-4o-mini",
            messages
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            var msg = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "The assistant could not authenticate. Please try again later."
                : response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    ? "Too many requests. Please wait a moment and try again."
                    : "The assistant is temporarily unavailable. Please try again later.";
            return StatusCode((int)response.StatusCode, new ChatResponse { Reply = msg });
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var reply = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "No response.";
        return Ok(new ChatResponse { Reply = reply.Trim() });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessage>? History { get; set; }
}

public class ChatMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
}
