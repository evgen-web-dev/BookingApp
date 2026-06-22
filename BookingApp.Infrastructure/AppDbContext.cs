using BookingApp.Domain;
using Microsoft.EntityFrameworkCore;

namespace BookingApp.Infrastructure;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<UserRole>().HasKey(role => new { role.UserId, role.Role });
        
        modelBuilder.Entity<UserRole>()
            .HasOne(role => role.User)
            .WithMany(user => user.UserRoles)
            .HasForeignKey(role => role.UserId);
    }
}