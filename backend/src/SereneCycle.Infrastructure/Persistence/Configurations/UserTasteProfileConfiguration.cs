using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SereneCycle.Domain.Entities;
using SereneCycle.Infrastructure.Identity;

namespace SereneCycle.Infrastructure.Persistence.Configurations;

public class UserTasteProfileConfiguration
    : IEntityTypeConfiguration<UserTasteProfile>
{
    /// <summary>
    /// Diziler yerinde değiştirildiği için EF'in referans karşılaştırması
    /// yetmez; bu karşılaştırıcı olmadan geri bildirim güncellemeleri
    /// sessizce kaybolur.
    /// </summary>
    private static readonly ValueComparer<short[]> CountsComparer = new(
        (left, right) => left != null && right != null
                         && left.SequenceEqual(right),
        counts => counts.Aggregate(0, HashCode.Combine),
        counts => counts.ToArray());

    public void Configure(EntityTypeBuilder<UserTasteProfile> builder)
    {
        builder.ToTable("user_taste_profiles");

        // Kullanıcı başına tek satır: birincil anahtar doğrudan kullanıcı
        // kimliği, böylece öneri isteği tek birincil anahtar getirisine iner.
        builder.HasKey(p => p.UserId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureCounts(builder.Property(p => p.Alpha));
        ConfigureCounts(builder.Property(p => p.Beta));
    }

    /// <summary>
    /// Postgres dizi uzunluğunu zorlamaz; sayaç dizilerinin
    /// <see cref="TasteTags.Capacity"/> uzunluğunda olması uygulama
    /// değişmezidir (<see cref="UserTasteProfile.CreateFor"/>).
    /// </summary>
    private static void ConfigureCounts(PropertyBuilder<short[]> property)
    {
        property
            .HasColumnType("smallint[]")
            .IsRequired()
            .Metadata.SetValueComparer(CountsComparer);
    }
}
