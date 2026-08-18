using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Infrastructure.Persistence.Configurations;

public class CycleRiskSummaryConfiguration
    : IEntityTypeConfiguration<CycleRiskSummary>
{
    public void Configure(EntityTypeBuilder<CycleRiskSummary> builder)
    {
        builder.ToTable("cycle_risk_summaries");

        // Döngü başına tek satır: birincil anahtar doğrudan döngü kimliği,
        // böylece ana sayfa okuması tek birincil anahtar getirisi olur.
        builder.HasKey(s => s.CycleId);

        // Döngü silinirse özeti de gitsin; yetim satır kalmasın.
        builder.HasOne<Cycle>()
            .WithMany()
            .HasForeignKey(s => s.CycleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Hesaplanmış özellikler değil, sayaçlar: hepsi sütun.
        builder.HasIndex(s => new { s.UserId, s.CycleStartDate })
            .HasDatabaseName("ix_cycle_risk_summaries_user_start");
    }
}
