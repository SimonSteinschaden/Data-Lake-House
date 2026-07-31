using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMeterAnnualValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AnnualValue",
                table: "Meters",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnnualValueOrigin",
                table: "Meters",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualValue",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "AnnualValueOrigin",
                table: "Meters");
        }
    }
}
