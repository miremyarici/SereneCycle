using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SereneCycle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CycleRiskSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SymptomMask",
                table: "daily_logs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Var olan kayıtların maskesi ara tablodan bir kerede doldurulur:
            // yeni kolon eski satırlarda 0 kalmasın, risk motoru geçmiş
            // günlerin semptomlarını da görsün.
            migrationBuilder.Sql(
                """
                UPDATE daily_logs AS d
                SET "SymptomMask" = s.mask
                FROM (
                    SELECT "LogId",
                           bit_or(1::bigint << ("SymptomId" - 1)) AS mask
                    FROM log_symptoms
                    WHERE "SymptomId" BETWEEN 1 AND 64
                    GROUP BY "LogId"
                ) AS s
                WHERE d."Id" = s."LogId";
                """);

            migrationBuilder.CreateTable(
                name: "cycle_risk_summaries",
                columns: table => new
                {
                    CycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LoggedDays = table.Column<int>(type: "integer", nullable: false),
                    BleedingDays = table.Column<int>(type: "integer", nullable: false),
                    LongestBleedingStreak = table.Column<int>(type: "integer", nullable: false),
                    HeavyDays = table.Column<int>(type: "integer", nullable: false),
                    LongestHeavyStreak = table.Column<int>(type: "integer", nullable: false),
                    SpottingDays = table.Column<int>(type: "integer", nullable: false),
                    SpottingOutsidePeriodDays = table.Column<int>(type: "integer", nullable: false),
                    BleedingRestartCount = table.Column<int>(type: "integer", nullable: false),
                    PainDays = table.Column<int>(type: "integer", nullable: false),
                    BloodColorMask = table.Column<int>(type: "integer", nullable: false),
                    SymptomMask = table.Column<long>(type: "bigint", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cycle_risk_summaries", x => x.CycleId);
                    table.ForeignKey(
                        name: "FK_cycle_risk_summaries_cycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "cycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cycle_risk_summaries_user_start",
                table: "cycle_risk_summaries",
                columns: new[] { "UserId", "CycleStartDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cycle_risk_summaries");

            migrationBuilder.DropColumn(
                name: "SymptomMask",
                table: "daily_logs");
        }
    }
}
