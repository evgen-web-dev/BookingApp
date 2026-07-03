using BookingApp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingApp.Infrastructure.EntityTypeConfigurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(role => new { role.UserId, role.Role });
        
        builder.HasOne(role => role.User)
            .WithMany(user => user.UserRoles)
            .HasForeignKey(role => role.UserId);
    }
}