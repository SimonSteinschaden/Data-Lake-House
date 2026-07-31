using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateGoldProfileReleaseStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"GoldProfileVersions\" SET \"ReleaseStatus\" = 'Archived' " +
                "WHERE \"ReleaseStatus\" IN ('Superseded', 'Revoked');");
            migrationBuilder.Sql(
                "UPDATE \"GoldProfileEvents\" SET \"PreviousStatus\" = 'Archived' " +
                "WHERE \"PreviousStatus\" IN ('Superseded', 'Revoked');");
            migrationBuilder.Sql(
                "UPDATE \"GoldProfileEvents\" SET \"NewStatus\" = 'Archived' " +
                "WHERE \"NewStatus\" IN ('Superseded', 'Revoked');");
            migrationBuilder.Sql(
                "UPDATE \"GoldProfileEvents\" SET \"EventType\" = 'Archived' " +
                "WHERE \"EventType\" IN ('Superseded', 'Revoked');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Der Rückbau kann Superseded und Revoked nicht unterscheidbar wiederherstellen,
            // da beide Werte additiv zu "Archived" konsolidiert wurden.
            migrationBuilder.Sql(
                "UPDATE \"GoldProfileVersions\" SET \"ReleaseStatus\" = 'Revoked' " +
                "WHERE \"ReleaseStatus\" = 'Archived';");
            migrationBuilder.Sql(
                "UPDATE \"GoldProfileEvents\" SET \"PreviousStatus\" = 'Revoked' " +
                "WHERE \"PreviousStatus\" = 'Archived';");
            migrationBuilder.Sql(
                "UPDATE \"GoldProfileEvents\" SET \"NewStatus\" = 'Revoked' " +
                "WHERE \"NewStatus\" = 'Archived';");
            migrationBuilder.Sql(
                "UPDATE \"GoldProfileEvents\" SET \"EventType\" = 'Revoked' " +
                "WHERE \"EventType\" = 'Archived';");
        }
    }
}
