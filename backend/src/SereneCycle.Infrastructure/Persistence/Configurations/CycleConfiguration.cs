using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Infrastructure.Persistence.Configurations;

public class CycleConfiguration : IEntityTypeConfiguration<Cycle>
{
    public void Configure(EntityTypeBuilder<Cycle> builder)
    {
        builder.ToTable("cycles");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.StartDate).IsRequired();

        // Hesaplanmış özellik; sütun olarak tutulmaz.
        builder.Ignore(c => c.LengthInDays);

        // Aynı gün iki kez adet başlangıcı kaydedilemesin. Bu indeks aynı
        // zamanda "kullanıcının döngülerini tarihe göre çek" sorgusunu karşılar.
        builder.HasIndex(c => new { c.UserId, c.StartDate })
            .IsUnique()
            .HasDatabaseName("ix_cycles_user_start_unique");
    }
}
