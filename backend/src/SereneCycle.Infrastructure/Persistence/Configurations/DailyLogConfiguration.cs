using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Infrastructure.Persistence.Configurations;

public class DailyLogConfiguration : IEntityTypeConfiguration<DailyLog>
{
    public void Configure(EntityTypeBuilder<DailyLog> builder)
    {
        builder.ToTable("daily_logs", t =>
            t.HasCheckConstraint(
                "ck_daily_logs_flow_range",
                "\"Flow\" IS NULL OR \"Flow\" BETWEEN 0 AND 3"));

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Note).HasMaxLength(2000);

        // Kan rengi de semptom kategorisi gibi okunabilir metin olarak
        // saklanır: enum sırası değişirse eski kayıtlar bozulmasın.
        builder.Property(l => l.BloodColor)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Gün başına tek kayıt; PUT /logs/{date} bunun üzerine upsert yapar.
        builder.HasIndex(l => new { l.UserId, l.LogDate })
            .IsUnique()
            .HasDatabaseName("ix_daily_logs_user_date_unique");
    }
}

public class SymptomConfiguration : IEntityTypeConfiguration<Symptom>
{
    public void Configure(EntityTypeBuilder<Symptom> builder)
    {
        builder.ToTable("symptoms");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(60);

        // Enum'ları okunabilir metin olarak sakla: veritabanına elle bakınca
        // anlaşılır olsun ve enum sırası değişirse veri bozulmasın.
        builder.Property(s => s.Category)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => s.Name).IsUnique();

        builder.HasData(SymptomSeedData.All);
    }
}

public class LogSymptomConfiguration : IEntityTypeConfiguration<LogSymptom>
{
    public void Configure(EntityTypeBuilder<LogSymptom> builder)
    {
        builder.ToTable("log_symptoms");

        builder.HasKey(ls => new { ls.LogId, ls.SymptomId });

        builder.HasOne(ls => ls.Log)
            .WithMany(l => l.LogSymptoms)
            .HasForeignKey(ls => ls.LogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ls => ls.Symptom)
            .WithMany(s => s.LogSymptoms)
            .HasForeignKey(ls => ls.SymptomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
