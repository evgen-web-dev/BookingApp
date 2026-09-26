using BookingApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingApp.Infrastructure.EntityTypeConfigurations;

public class ApartmentEntityTypeConfiguration : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        builder.ToTable("Apartments");
        
        builder.HasKey(s => s.Id);

        builder.Property(apartment => apartment.Price)
            .HasPrecision(18, 2)
            .IsRequired();
        
        builder.Property(apartment => apartment.Capacity)
            .IsRequired();

        builder.Property(apartment => apartment.Title)
            .IsRequired()
            .HasMaxLength(150);
        
        builder.Property(apartment => apartment.Location)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(apartment => apartment.Description)
            .HasMaxLength(600);
        
        builder.HasOne<User>(apartment => apartment.Owner)
            .WithMany()
            .HasForeignKey(apartment => apartment.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}