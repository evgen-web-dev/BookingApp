using BookingApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingApp.Infrastructure.EntityTypeConfigurations;

public class SessionEntityTypeConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        
        builder.HasKey(s => s.Id);
        
        builder.HasOne<User>(session => session.User)
            .WithMany(user => user.Sessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Prop of DateTime type is by default Npgsql mapped into "timestamp without time zone" type in postgresql.
        // If to save data of DateTime + Kind=UTC type into "timestamp without time zone" - exception will be thrown.
        // So to correctly store DateTime + Kind=UTC data - need to have "timestamp with time zone" type in postgresql for it explicitly.
        builder.Property(session => session.AbsoluteExpiresAt)
            .HasColumnType("timestamp with time zone");
        builder.Property(session => session.CreatedAt)
            .HasColumnType("timestamp with time zone");
        builder.Property(session => session.RevokedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(session => session.RevokedReason)
            .HasConversion<string>()
            .HasMaxLength(32);
    }
}