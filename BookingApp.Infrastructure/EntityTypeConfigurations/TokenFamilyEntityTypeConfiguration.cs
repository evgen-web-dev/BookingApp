using BookingApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingApp.Infrastructure.EntityTypeConfigurations;

public class TokenFamilyEntityTypeConfiguration : IEntityTypeConfiguration<TokenFamily>
{
    public void Configure(EntityTypeBuilder<TokenFamily> builder)
    {
        builder.ToTable("TokenFamilies");
        
        builder.HasKey(s => s.Id);
        
        builder.HasOne<User>(tokenFamily => tokenFamily.User)
            .WithMany(user => user.TokenFamilies)
            .HasForeignKey(tokenFamily => tokenFamily.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Prop of DateTime type is by default Npgsql mapped into "timestamp without time zone" type in postgresql.
        // If to save data of DateTime + Kind=UTC type into "timestamp without time zone" - exception will be thrown.
        // So to correctly store DateTime + Kind=UTC data - need to have "timestamp with time zone" type in postgresql for it explicitly.
        builder.Property(tokenFamily => tokenFamily.AbsoluteExpiresAt)
            .HasColumnType("timestamp with time zone");
        builder.Property(tokenFamily => tokenFamily.CreatedAt)
            .HasColumnType("timestamp with time zone");
        builder.Property(tokenFamily => tokenFamily.RevokedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(tokenFamily => tokenFamily.RevokedReason)
            .HasConversion<string>()
            .HasMaxLength(32);
    }
}