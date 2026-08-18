using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SereneCycle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PeriodLogAndProfileEditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BloodColor",
                table: "daily_logs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBleeding",
                table: "daily_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSpotting",
                table: "daily_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AvatarContentType",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "AvatarData",
                table: "AspNetUsers",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AvatarUpdatedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailChangeAttemptCount",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailChangeCodeExpiresAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailChangeCodeHash",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingEmail",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Karın krampları");

            migrationBuilder.UpdateData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Göğüs ağrısı");

            migrationBuilder.UpdateData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "Akne");

            migrationBuilder.UpdateData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 8,
                column: "Name",
                value: "İştah değişikliği");

            migrationBuilder.InsertData(
                table: "symptoms",
                columns: new[] { "Id", "Category", "Name" },
                values: new object[,]
                {
                    { 9, "Physical", "Ateş basması" },
                    { 10, "Physical", "Bulantı" },
                    { 11, "Physical", "Cilt kuruluğu" },
                    { 12, "Physical", "Gece terlemeleri" },
                    { 13, "Physical", "Hafıza kaybı" },
                    { 14, "Physical", "İdrar kaçırma" },
                    { 15, "Physical", "İshal" },
                    { 16, "Physical", "Kabızlık" },
                    { 17, "Physical", "Pelvis ağrısı" },
                    { 18, "Physical", "Saç dökülmesi" },
                    { 19, "Physical", "Uyku değişiklikleri" },
                    { 30, "Physical", "Üşüme" },
                    { 31, "Physical", "Vajina kuruluğu" },
                    { 32, "Mood", "Duygudurum değişiklikleri" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DropColumn(
                name: "BloodColor",
                table: "daily_logs");

            migrationBuilder.DropColumn(
                name: "HasBleeding",
                table: "daily_logs");

            migrationBuilder.DropColumn(
                name: "HasSpotting",
                table: "daily_logs");

            migrationBuilder.DropColumn(
                name: "AvatarContentType",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AvatarData",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AvatarUpdatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailChangeAttemptCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailChangeCodeExpiresAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailChangeCodeHash",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PendingEmail",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Kramp");

            migrationBuilder.UpdateData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Göğüs hassasiyeti");

            migrationBuilder.UpdateData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "Sivilce");

            migrationBuilder.UpdateData(
                table: "symptoms",
                keyColumn: "Id",
                keyValue: 8,
                column: "Name",
                value: "İştah değişimi");
        }
    }
}
