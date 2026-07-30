using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMeterReadingMassImportStaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportStagingMeterReadings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<int>(type: "integer", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeterNumberOriginal = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Value = table.Column<decimal>(type: "numeric", nullable: true),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    QualityFlag = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ReadingType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EnergyDirection = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IntervalSeconds = table.Column<int>(type: "integer", nullable: true),
                    ValidationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ValidationCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ValidationMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RawSourceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportStagingMeterReadings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeterReadingImportAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadCount = table.Column<long>(type: "bigint", nullable: false),
                    WrittenCount = table.Column<long>(type: "bigint", nullable: false),
                    RejectedCount = table.Column<long>(type: "bigint", nullable: false),
                    DuplicateCount = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterReadingImportAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeterReadingImportJobs",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetMode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Phase = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalBytes = table.Column<long>(type: "bigint", nullable: false),
                    ProcessedBytes = table.Column<long>(type: "bigint", nullable: false),
                    ReadRows = table.Column<long>(type: "bigint", nullable: false),
                    StagedRows = table.Column<long>(type: "bigint", nullable: false),
                    ValidRows = table.Column<long>(type: "bigint", nullable: false),
                    RejectedRows = table.Column<long>(type: "bigint", nullable: false),
                    DuplicateRows = table.Column<long>(type: "bigint", nullable: false),
                    WrittenRows = table.Column<long>(type: "bigint", nullable: false),
                    CurrentBatch = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CancellationRequested = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterReadingImportJobs", x => x.JobId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportStagingMeterReadings_ImportId",
                table: "ImportStagingMeterReadings",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportStagingMeterReadings_ImportId_BatchNumber",
                table: "ImportStagingMeterReadings",
                columns: new[] { "ImportId", "BatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportStagingMeterReadings_ImportId_MeterId_Timestamp",
                table: "ImportStagingMeterReadings",
                columns: new[] { "ImportId", "MeterId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportStagingMeterReadings_ImportId_ValidationStatus",
                table: "ImportStagingMeterReadings",
                columns: new[] { "ImportId", "ValidationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportStagingMeterReadings_MeterId_Timestamp",
                table: "ImportStagingMeterReadings",
                columns: new[] { "MeterId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadingImportAudits_ImportId",
                table: "MeterReadingImportAudits",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadingImportAudits_ImportId_MeterId",
                table: "MeterReadingImportAudits",
                columns: new[] { "ImportId", "MeterId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadingImportJobs_ImportId",
                table: "MeterReadingImportJobs",
                column: "ImportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadingImportJobs_Status",
                table: "MeterReadingImportJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportStagingMeterReadings");

            migrationBuilder.DropTable(
                name: "MeterReadingImportAudits");

            migrationBuilder.DropTable(
                name: "MeterReadingImportJobs");
        }
    }
}
