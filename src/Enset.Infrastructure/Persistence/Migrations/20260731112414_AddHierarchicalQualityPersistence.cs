using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHierarchicalQualityPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "EntityAuditHistory",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorType",
                table: "EntityAuditHistory",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayNameSnapshot",
                table: "EntityAuditHistory",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperationId",
                table: "EntityAuditHistory",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedAnalysisId",
                table: "EntityAuditHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedIssueId",
                table: "EntityAuditHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BuildingInventoryDeclarations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterInventoryComplete = table.Column<bool>(type: "boolean", nullable: false),
                    EnergySystemInventoryComplete = table.Column<bool>(type: "boolean", nullable: false),
                    NoRelevantEnergySystemsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedByDisplayNameSnapshot = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InvalidatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvalidatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvalidationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingInventoryDeclarations", x => x.Id);
                    table.CheckConstraint("CK_BuildingInventoryDeclarations_VersionNumber", "\"VersionNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_BuildingInventoryDeclarations_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeterProfileAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    PeriodFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnalysisStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AnalysisVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpectedReadingCount = table.Column<long>(type: "bigint", nullable: false),
                    ActualReadingCount = table.Column<long>(type: "bigint", nullable: false),
                    CompletenessPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    GapCount = table.Column<long>(type: "bigint", nullable: false),
                    AnomalyCount = table.Column<long>(type: "bigint", nullable: false),
                    BlockingIssueCount = table.Column<long>(type: "bigint", nullable: false),
                    WarningCount = table.Column<long>(type: "bigint", nullable: false),
                    DetectedIntervalSeconds = table.Column<int>(type: "integer", nullable: true),
                    DetectedUnit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ExecutedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecutedByActorType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExecutedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ExecutedByDisplayNameSnapshot = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    SupersededAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterProfileAnalyses", x => x.Id);
                    table.CheckConstraint("CK_MeterProfileAnalyses_Completeness", "\"CompletenessPercentage\" >= 0 AND \"CompletenessPercentage\" <= 100");
                    table.CheckConstraint("CK_MeterProfileAnalyses_Period", "\"PeriodFromUtc\" < \"PeriodToUtc\"");
                    table.CheckConstraint("CK_MeterProfileAnalyses_VersionNumber", "\"VersionNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_MeterProfileAnalyses_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeterProfileIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterProfileAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TimestampFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimestampToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: true),
                    OriginalValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ExpectedValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TechnicalDetails = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    IsBlocking = table.Column<bool>(type: "boolean", nullable: false),
                    ResolutionStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterProfileIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeterProfileIssues_MeterProfileAnalyses_MeterProfileAnalysi~",
                        column: x => x.MeterProfileAnalysisId,
                        principalTable: "MeterProfileAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeterProfileIssues_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeterProfileCurationDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterProfileIssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterProfileAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreviousValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    GeneratedValueMethod = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ConfidencePercent = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecidedByDisplayNameSnapshot = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsFinal = table.Column<bool>(type: "boolean", nullable: false),
                    SupersedesDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterProfileCurationDecisions", x => x.Id);
                    table.CheckConstraint("CK_MeterProfileCurationDecisions_Confidence", "\"ConfidencePercent\" IS NULL OR (\"ConfidencePercent\" >= 0 AND \"ConfidencePercent\" <= 100)");
                    table.ForeignKey(
                        name: "FK_MeterProfileCurationDecisions_MeterProfileAnalyses_MeterPro~",
                        column: x => x.MeterProfileAnalysisId,
                        principalTable: "MeterProfileAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeterProfileCurationDecisions_MeterProfileCurationDecisions~",
                        column: x => x.SupersedesDecisionId,
                        principalTable: "MeterProfileCurationDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeterProfileCurationDecisions_MeterProfileIssues_MeterProfi~",
                        column: x => x.MeterProfileIssueId,
                        principalTable: "MeterProfileIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeterProfileCurationDecisions_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditHistory_OperationId",
                table: "EntityAuditHistory",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingInventoryDeclarations_BuildingId_VersionNumber",
                table: "BuildingInventoryDeclarations",
                columns: new[] { "BuildingId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BuildingInventoryDeclarations_Current",
                table: "BuildingInventoryDeclarations",
                column: "BuildingId",
                unique: true,
                filter: "\"IsCurrent\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileAnalyses_AnalysisStatus",
                table: "MeterProfileAnalyses",
                column: "AnalysisStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileAnalyses_MeterId_VersionNumber",
                table: "MeterProfileAnalyses",
                columns: new[] { "MeterId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileAnalyses_PeriodFromUtc_PeriodToUtc",
                table: "MeterProfileAnalyses",
                columns: new[] { "PeriodFromUtc", "PeriodToUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_MeterProfileAnalyses_Current",
                table: "MeterProfileAnalyses",
                column: "MeterId",
                unique: true,
                filter: "\"IsCurrent\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileCurationDecisions_DecidedAtUtc",
                table: "MeterProfileCurationDecisions",
                column: "DecidedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileCurationDecisions_DecidedByUserId",
                table: "MeterProfileCurationDecisions",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileCurationDecisions_MeterId",
                table: "MeterProfileCurationDecisions",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileCurationDecisions_MeterProfileAnalysisId",
                table: "MeterProfileCurationDecisions",
                column: "MeterProfileAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileCurationDecisions_MeterProfileIssueId",
                table: "MeterProfileCurationDecisions",
                column: "MeterProfileIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileCurationDecisions_SupersedesDecisionId",
                table: "MeterProfileCurationDecisions",
                column: "SupersedesDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileIssues_IsBlocking",
                table: "MeterProfileIssues",
                column: "IsBlocking");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileIssues_MeterId",
                table: "MeterProfileIssues",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileIssues_MeterProfileAnalysisId",
                table: "MeterProfileIssues",
                column: "MeterProfileAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileIssues_MeterProfileAnalysisId_ResolutionStatus",
                table: "MeterProfileIssues",
                columns: new[] { "MeterProfileAnalysisId", "ResolutionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileIssues_ResolutionStatus",
                table: "MeterProfileIssues",
                column: "ResolutionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MeterProfileIssues_Severity",
                table: "MeterProfileIssues",
                column: "Severity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildingInventoryDeclarations");

            migrationBuilder.DropTable(
                name: "MeterProfileCurationDecisions");

            migrationBuilder.DropTable(
                name: "MeterProfileIssues");

            migrationBuilder.DropTable(
                name: "MeterProfileAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_EntityAuditHistory_OperationId",
                table: "EntityAuditHistory");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "EntityAuditHistory");

            migrationBuilder.DropColumn(
                name: "ActorType",
                table: "EntityAuditHistory");

            migrationBuilder.DropColumn(
                name: "DisplayNameSnapshot",
                table: "EntityAuditHistory");

            migrationBuilder.DropColumn(
                name: "OperationId",
                table: "EntityAuditHistory");

            migrationBuilder.DropColumn(
                name: "RelatedAnalysisId",
                table: "EntityAuditHistory");

            migrationBuilder.DropColumn(
                name: "RelatedIssueId",
                table: "EntityAuditHistory");
        }
    }
}
