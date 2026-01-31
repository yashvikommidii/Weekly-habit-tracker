namespace WeeklyHabitTracker.Models;

public class HabitEntry
{
    public int Id { get; set; }
    public int HabitId { get; set; }
    public string Date { get; set; } = string.Empty;  // ISO date "yyyy-MM-dd"
    public bool Completed { get; set; }  // true = did it, false = didn't do it
}
