using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedMeterReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceRawReadingId",
                table: "MeterReadings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportedMeterReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeterNumberRaw = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TimestampRaw = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ValueRaw = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    QualityRaw = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Value = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    Quality = table.Column<int>(type: "integer", nullable: true),
                    RowNumber = table.Column<int>(type: "integer", nullable: true),
                    SourceName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ParsingError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedMeterReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedMeterReadings_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_SourceImportJobId",
                table: "MeterReadings",
                column: "SourceImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_SourceRawReadingId",
                table: "MeterReadings",
                column: "SourceRawReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedMeterReadings_ImportId",
                table: "ImportedMeterReadings",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedMeterReadings_ImportId_RowNumber",
                table: "ImportedMeterReadings",
                columns: new[] { "ImportId", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportedMeterReadings_MeterId",
                table: "ImportedMeterReadings",
                column: "MeterId");

            migrationBuilder.AddForeignKey(
                name: "FK_MeterReadings_ImportedMeterReadings_SourceRawReadingId",
                table: "MeterReadings",
                column: "SourceRawReadingId",
                principalTable: "ImportedMeterReadings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MeterReadings_ImportedMeterReadings_SourceRawReadingId",
                table: "MeterReadings");

            migrationBuilder.DropTable(
                name: "ImportedMeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_SourceImportJobId",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_SourceRawReadingId",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "SourceRawReadingId",
                table: "MeterReadings");
        }
    }
}
