using Microsoft.EntityFrameworkCore;
using ease_intro_api.Models;

namespace ease_intro_api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Meet> Meets { get; set; }
    public DbSet<MeetStatus> MeetStatus { get; set; }
    public DbSet<Member> Member { get; set; }
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeetStatus>().HasData(
            new MeetStatus { Id = 1, Title = "Встреча запланирована", Description = "Встреча запланирована" },
            new MeetStatus { Id = 2, Title = "Встреча в процессе", Description = "Встреча в процессе" },
            new MeetStatus { Id = 3, Title = "Встреча завершена", Description = "Встреча завершена" },
            new MeetStatus { Id = 4, Title = "Встреча отменена", Description = "Встреча отменена" },
            new MeetStatus { Id = 5, Title = "Открыто для регистрации", Description = "Открыто для регистрации" }
        );
        
        modelBuilder.Entity<Meet>()
            .HasMany(m => m.Members)
            .WithOne(m => m.Meet)
            .HasForeignKey(m => m.MeetGuid);
        
        
        modelBuilder.Entity<Member>()
            .HasIndex(u => u.Contact)
            .IsUnique();
    }
}