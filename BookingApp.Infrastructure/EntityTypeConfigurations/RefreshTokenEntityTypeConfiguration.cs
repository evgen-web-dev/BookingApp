using BookingApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingApp.Infrastructure.EntityTypeConfigurations;

public class RefreshTokenEntityTypeConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        
        builder.HasKey(token => token.Id);
        
        builder.HasOne<Session>(token => token.Session)
            .WithMany(session => session.RefreshTokens)
            .HasForeignKey(token => token.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(64);
        builder.HasIndex(token => token.TokenHash)
            .IsUnique();
 
        // Prop of DateTime type is by default Npgsql mapped into "timestamp without time zone" type in postgresql.
        // If to save data of DateTime + Kind=UTC type into "timestamp without time zone" - exception will be thrown.
        // So to correctly store DateTime + Kind=UTC data - need to have "timestamp with time zone" type in postgresql for it explicitly.
        builder.Property(token => token.CreatedAt)
            .HasColumnType("timestamp with time zone");
        builder.Property(token => token.ExpiresAt)
            .HasColumnType("timestamp with time zone");
        builder.Property(token => token.RevokedAt)
            .HasColumnType("timestamp with time zone");
        
        builder.HasIndex(refreshToken => refreshToken.SessionId)
            .IsUnique()
            .HasFilter("\"RevokedAt\" IS NULL");
    }
}