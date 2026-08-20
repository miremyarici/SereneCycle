using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SereneCycle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContentTagMasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ContraMask",
                table: "content_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "content_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TagMask",
                table: "content_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "AvoidMask",
                table: "AspNetUsers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContraMask",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "TagMask",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "AvoidMask",
                table: "AspNetUsers");
        }
    }
}
