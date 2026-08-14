using BookingApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingApp.Infrastructure.EntityTypeConfigurations;

public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Targets same index that is crated by Identity's own model configuration. 
        // EF Core identifies indexes by the props they are indexing so here we are not creating new, 2-nd index next to 
        // Identity's "EmailIndex", we are creating new "EmailIndex" partial index with unique constraint
        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasFilter("\"NormalizedEmail\" IS NOT NULL");
    }
}