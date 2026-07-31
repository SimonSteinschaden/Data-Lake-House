using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureAustriaReferenceCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                sql:
                """
                INSERT INTO "Countries"
                    ("Id", "IsoCode2", "IsoCode3", "Name", "CreatedAt", "UpdatedAt")
                SELECT
                    '10000000-0000-0000-0000-000000000043',
                    'AT',
                    'AUT',
                    'Österreich',
                    TIMESTAMPTZ '1970-01-01 00:00:00+00',
                    NULL
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Countries"
                    WHERE "IsoCode2" = 'AT' OR "IsoCode3" = 'AUT'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Referenzdaten werden bewusst nicht automatisch entfernt:
            // Gebäudeadressen können inzwischen darauf verweisen.
        }
    }
}
