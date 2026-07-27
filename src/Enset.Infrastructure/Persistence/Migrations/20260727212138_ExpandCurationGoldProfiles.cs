using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandCurationGoldProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RuleId",
                table: "CurationTasks",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RuleVersion",
                table: "CurationTasks",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SuggestedNormalizedValue",
                table: "CurationTasks",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CuratedFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OriginalValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CuratedValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    NormalizedValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MaturityLevel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ConfidencePercent = table.Column<int>(type: "integer", nullable: false),
                    RuleId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RuleVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuratedFieldValues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuratedFieldValues_EntityType_EntityId_FieldName_ValidToUtc",
                table: "CuratedFieldValues",
                columns: new[] { "EntityType", "EntityId", "FieldName", "ValidToUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuratedFieldValues");

            migrationBuilder.DropColumn(
                name: "RuleId",
                table: "CurationTasks");

            migrationBuilder.DropColumn(
                name: "RuleVersion",
                table: "CurationTasks");

            migrationBuilder.DropColumn(
                name: "SuggestedNormalizedValue",
                table: "CurationTasks");
        }
    }
}
