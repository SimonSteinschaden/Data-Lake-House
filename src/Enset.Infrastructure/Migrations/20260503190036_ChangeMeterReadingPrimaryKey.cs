using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeMeterReadingPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_MeterId_Timestamp",
                table: "MeterReadings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_MeterId_Timestamp",
                table: "MeterReadings",
                columns: new[] { "MeterId", "Timestamp" });
        }
    }
}
