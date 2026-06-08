using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meters_Buildings_BuildingId",
                table: "Meters");

            migrationBuilder.DropTable(
                name: "DataSources");

            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.AlterColumn<Guid>(
                name: "BuildingId",
                table: "Meters",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "MeterNumber",
                table: "Meters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Meters_MeterNumber",
                table: "Meters",
                column: "MeterNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Meters_Buildings_BuildingId",
                table: "Meters",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meters_Buildings_BuildingId",
                table: "Meters");

            migrationBuilder.DropIndex(
                name: "IX_Meters_MeterNumber",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "MeterNumber",
                table: "Meters");

            migrationBuilder.AlterColumn<Guid>(
                name: "BuildingId",
                table: "Meters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DataSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_ProjectId",
                table: "ImportJobs",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meters_Buildings_BuildingId",
                table: "Meters",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
