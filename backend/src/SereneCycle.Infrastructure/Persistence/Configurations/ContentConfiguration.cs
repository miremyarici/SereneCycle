using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SereneCycle.Domain.Entities;
using SereneCycle.Infrastructure.Identity;

namespace SereneCycle.Infrastructure.Persistence.Configurations;

public class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("content_items");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Phase)
            .HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(c => c.Type)
            .HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(c => c.Title).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Body).IsRequired().HasMaxLength(500);

        // GET /content?phase=&type= bu indeksi kullanır.
        builder.HasIndex(c => new { c.Phase, c.Type });
    }
}

public class FoodPhaseScoreConfiguration
    : IEntityTypeConfiguration<FoodPhaseScore>
{
    public void Configure(EntityTypeBuilder<FoodPhaseScore> builder)
    {
        builder.ToTable("food_phase_scores");

        builder.HasKey(f => new { f.FoodClass, f.Phase });

        builder.Property(f => f.FoodClass).HasMaxLength(80);

        builder.Property(f => f.Phase)
            .HasConversion<string>().HasMaxLength(20);

        // Score -1/0/1 olarak saklanır (enum'un sayısal değeri anlamlı).
        builder.Property(f => f.Score).HasConversion<int>();

        builder.Property(f => f.Reason).IsRequired().HasMaxLength(300);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(200);

        builder.Ignore(t => t.IsActive);

        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.Name).IsRequired().HasMaxLength(60);
        builder.Property(u => u.VerificationCodeHash).HasMaxLength(200);
    }
}
