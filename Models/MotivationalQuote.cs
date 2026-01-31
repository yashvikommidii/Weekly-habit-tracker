namespace WeeklyHabitTracker.Models;

public class MotivationalQuote
{
    public int Id { get; set; }
    public string Quote { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
