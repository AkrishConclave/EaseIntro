using Microsoft.EntityFrameworkCore;
using ease_intro_api.Models;

namespace ease_intro_api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Meet> Meets { get; set; }
    public DbSet<MeetStatus> MeetStatus { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeetStatus>().HasData(
            new MeetStatus { Id = 1, Title = "Встреча запланирована", Description = "Встреча запланирована" },
            new MeetStatus { Id = 2, Title = "Встреча в процессе", Description = "Встреча в процессе" },
            new MeetStatus { Id = 3, Title = "Встреча завершена", Description = "Встреча завершена" },
            new MeetStatus { Id = 4, Title = "Встреча отменена", Description = "Встреча отменена" }
        );
    }
}