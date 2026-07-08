using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureMeterReadingTimescale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MeterReadings",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_MeterId",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MeterReadings");

            migrationBuilder.AddColumn<Guid>(
                name: "BuildingId",
                table: "MeterReadings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "MeterReadings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceImportJobId",
                table: "MeterReadings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MeterReadings",
                table: "MeterReadings",
                columns: new[] { "MeterId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_MeterId_Timestamp",
                table: "MeterReadings",
                columns: new[] { "MeterId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_Timestamp",
                table: "MeterReadings",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MeterReadings",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_MeterId_Timestamp",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_Timestamp",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "BuildingId",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "SourceImportJobId",
                table: "MeterReadings");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "MeterReadings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MeterReadings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeterReadings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MeterReadings",
                table: "MeterReadings",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_MeterId",
                table: "MeterReadings",
                column: "MeterId");
        }
    }
}
