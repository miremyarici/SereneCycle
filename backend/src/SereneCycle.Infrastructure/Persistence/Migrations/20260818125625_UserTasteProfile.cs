using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SereneCycle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserTasteProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_taste_profiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Alpha = table.Column<short[]>(type: "smallint[]", nullable: false),
                    Beta = table.Column<short[]>(type: "smallint[]", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_taste_profiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_user_taste_profiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_taste_profiles");
        }
    }
}
