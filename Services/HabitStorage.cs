using System.Text.Json;
using WeeklyHabitTracker.Models;

namespace WeeklyHabitTracker.Services;

public class HabitStorage
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private List<Habit> _habits = new();
    private List<HabitEntry> _entries = new();
    private int _nextHabitId = 1;
    private int _nextEntryId = 1;

    public HabitStorage(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "habits.json");
        Load();
    }

    public List<Habit> Habits => _habits;
    public List<HabitEntry> Entries => _entries;

    public int NextHabitId() => _nextHabitId++;
    public int NextEntryId() => _nextEntryId++;

    public void AddHabit(Habit habit)
    {
        lock (_lock)
        {
            _habits.Add(habit);
            Save();
        }
    }

    public void AddOrUpdateEntry(HabitEntry entry)
    {
        lock (_lock)
        {
            var existing = _entries.FirstOrDefault(e => e.HabitId == entry.HabitId && e.Date == entry.Date);
            if (existing != null)
                existing.Completed = entry.Completed;
            else
                _entries.Add(entry);
            Save();
        }
    }

    private void Load()
    {
        if (!File.Exists(_filePath)) return;
        try
        {
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<StorageData>(json);
            if (data == null) return;
            _habits = data.Habits ?? new List<Habit>();
            _entries = data.Entries ?? new List<HabitEntry>();
            _nextHabitId = data.NextHabitId > 0 ? data.NextHabitId : (_habits.Count > 0 ? _habits.Max(h => h.Id) + 1 : 1);
            _nextEntryId = data.NextEntryId > 0 ? data.NextEntryId : (_entries.Count > 0 ? _entries.Max(e => e.Id) + 1 : 1);
        }
        catch { /* keep default empty data */ }
    }

    private void Save()
    {
        try
        {
            var data = new StorageData
            {
                Habits = _habits,
                Entries = _entries,
                NextHabitId = _nextHabitId,
                NextEntryId = _nextEntryId
            };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { /* ignore save errors */ }
    }

    private class StorageData
    {
        public List<Habit> Habits { get; set; } = new();
        public List<HabitEntry> Entries { get; set; } = new();
        public int NextHabitId { get; set; }
        public int NextEntryId { get; set; }
    }
}
