using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurationCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurationTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OriginalValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SuggestedValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ConfidencePercent = table.Column<int>(type: "integer", nullable: false),
                    Reasoning = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CuratedValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurationTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurationDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurationTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OriginalValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SuggestedValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    NewValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ConfidencePercent = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurationDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurationDecisions_CurationTasks_CurationTaskId",
                        column: x => x.CurationTaskId,
                        principalTable: "CurationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurationDecisions_CurationTaskId_DecidedAtUtc",
                table: "CurationDecisions",
                columns: new[] { "CurationTaskId", "DecidedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CurationTasks_EntityType_EntityId_FieldName",
                table: "CurationTasks",
                columns: new[] { "EntityType", "EntityId", "FieldName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurationDecisions");

            migrationBuilder.DropTable(
                name: "CurationTasks");
        }
    }
}
