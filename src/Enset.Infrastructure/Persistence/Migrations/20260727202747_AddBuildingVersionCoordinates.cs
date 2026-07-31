using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingVersionCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "BuildingVersions",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "BuildingVersions",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "BuildingVersions");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "BuildingVersions");
        }
    }
}
