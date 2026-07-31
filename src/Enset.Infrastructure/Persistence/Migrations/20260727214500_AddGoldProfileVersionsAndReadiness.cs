using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoldProfileVersionsAndReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoldProfileVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    ProfileType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCurationRevision = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    ReleaseStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ReleaseReason = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldProfileVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldProfileEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoldProfileVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    SnapshotHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldProfileEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldProfileEvents_GoldProfileVersions_GoldProfileVersionId",
                        column: x => x.GoldProfileVersionId,
                        principalTable: "GoldProfileVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoldProfileEvents_GoldProfileVersionId",
                table: "GoldProfileEvents",
                column: "GoldProfileVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldProfileVersions_EntityType_EntityId_IsCurrent",
                table: "GoldProfileVersions",
                columns: new[] { "EntityType", "EntityId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldProfileVersions_EntityType_EntityId_VersionNumber",
                table: "GoldProfileVersions",
                columns: new[] { "EntityType", "EntityId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldProfileVersions_ReleaseStatus",
                table: "GoldProfileVersions",
                column: "ReleaseStatus");

            migrationBuilder.CreateIndex(
                name: "IX_GoldProfileVersions_SnapshotHash",
                table: "GoldProfileVersions",
                column: "SnapshotHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoldProfileEvents");

            migrationBuilder.DropTable(
                name: "GoldProfileVersions");
        }
    }
}
