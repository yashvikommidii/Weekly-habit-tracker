using Microsoft.EntityFrameworkCore;
using WeeklyHabitTracker.Models;

namespace WeeklyHabitTracker.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.MotivationalQuotes.AnyAsync()) return;

        var quotes = new[]
        {
            new MotivationalQuote { Quote = "We are what we repeatedly do. Excellence, then, is not an act, but a habit.", Author = "Aristotle" },
            new MotivationalQuote { Quote = "The secret of getting ahead is getting started.", Author = "Mark Twain" },
            new MotivationalQuote { Quote = "Small daily improvements over time lead to stunning results.", Author = "Robin Sharma" },
            new MotivationalQuote { Quote = "You don't have to be great to start, but you have to start to be great.", Author = "Zig Ziglar" },
            new MotivationalQuote { Quote = "The best time to plant a tree was 20 years ago. The second best time is now.", Author = "Chinese Proverb" }
        };
        db.MotivationalQuotes.AddRange(quotes);
        await db.SaveChangesAsync();
    }
}
