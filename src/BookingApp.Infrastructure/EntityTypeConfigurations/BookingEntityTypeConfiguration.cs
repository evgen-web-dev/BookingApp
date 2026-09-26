using BookingApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingApp.Infrastructure.EntityTypeConfigurations;

public class BookingEntityTypeConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        
        builder.HasKey(s => s.Id);
        
        builder.HasOne<Apartment>(booking => booking.Apartment)
            .WithMany(apartment => apartment.Bookings)
            .HasForeignKey(booking => booking.ApartmentId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne<User>(booking => booking.Client)
            .WithMany()
            .HasForeignKey(booking => booking.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(booking => new { booking.ApartmentId, booking.CheckIn, booking.CheckOut } );
        
        // .CheckIn / .CheckOut is a prop of DateTime on the C# entity level, but we will need to store only date (yyyy-mm-dd) in DB
        // so we set type to "date" explicitly
        builder.Property(booking => booking.CheckIn)
            .HasColumnType("date");
        builder.Property(booking => booking.CheckOut)
            .HasColumnType("date");
        
        // Prop of DateTime type is by default Npgsql mapped into "timestamp without time zone" type in postgresql.
        // If to save data of DateTime + Kind=UTC type into "timestamp without time zone" - exception will be thrown.
        // So to correctly store DateTime + Kind=UTC data - need to have "timestamp with time zone" type in postgresql for it explicitly.
        builder.Property(booking => booking.CreatedAt)
            .HasColumnType("timestamp with time zone");
    }
}