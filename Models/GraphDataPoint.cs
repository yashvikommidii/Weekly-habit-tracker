namespace WeeklyHabitTracker.Models;

public class GraphDataPoint
{
    public string Day { get; set; } = string.Empty;
    public int CompletedCount { get; set; }
    public int TotalHabits { get; set; }
}
