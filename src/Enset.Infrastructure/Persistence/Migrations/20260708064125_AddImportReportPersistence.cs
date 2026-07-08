using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportReportPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportReports",
                columns: table => new
                {
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceFileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SourceFileContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SourceFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    SourceFileSha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SourceFileStagedPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    SourceFileRawStoragePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CustomerCount = table.Column<int>(type: "integer", nullable: false),
                    BuildingCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportReports", x => x.ImportId);
                });

            migrationBuilder.CreateTable(
                name: "ImportAuditEntries",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreviousResolutionAction = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ResolutionAction = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PreviousCustomResolvedValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CustomResolvedValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportAuditEntries", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK_ImportAuditEntries_ImportReports_ImportId",
                        column: x => x.ImportId,
                        principalTable: "ImportReports",
                        principalColumn: "ImportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportIssues",
                columns: table => new
                {
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SimilarityScore = table.Column<double>(type: "double precision", nullable: true),
                    RequiresUserDecision = table.Column<bool>(type: "boolean", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FirstValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    SecondValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ResolutionAction = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CustomResolvedValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportIssues", x => x.IssueId);
                    table.ForeignKey(
                        name: "FK_ImportIssues_ImportReports_ImportId",
                        column: x => x.ImportId,
                        principalTable: "ImportReports",
                        principalColumn: "ImportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportAuditEntries_ImportId",
                table: "ImportAuditEntries",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportAuditEntries_IssueId",
                table: "ImportAuditEntries",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportIssues_ImportId",
                table: "ImportIssues",
                column: "ImportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportAuditEntries");

            migrationBuilder.DropTable(
                name: "ImportIssues");

            migrationBuilder.DropTable(
                name: "ImportReports");
        }
    }
}
