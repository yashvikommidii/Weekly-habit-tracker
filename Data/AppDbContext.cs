using Microsoft.EntityFrameworkCore;
using WeeklyHabitTracker.Models;

namespace WeeklyHabitTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<MotivationalQuote> MotivationalQuotes => Set<MotivationalQuote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MotivationalQuote>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Quote).HasMaxLength(500).IsRequired();
            e.Property(x => x.Author).HasMaxLength(100).IsRequired();
        });
    }
}
