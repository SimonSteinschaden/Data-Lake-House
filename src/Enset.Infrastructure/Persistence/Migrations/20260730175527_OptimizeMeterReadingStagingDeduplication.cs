using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeMeterReadingStagingDeduplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ImportStagingMeterReadings_ImportId_MeterId_Timestamp_Sourc~",
                table: "ImportStagingMeterReadings",
                columns: new[] { "ImportId", "MeterId", "Timestamp", "SourceRowNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportStagingMeterReadings_ImportId_MeterId_Timestamp_Sourc~",
                table: "ImportStagingMeterReadings");
        }
    }
}
