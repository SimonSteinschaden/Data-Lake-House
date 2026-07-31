using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "BuildingNumberSequence");

            migrationBuilder.Sql(
                sql:
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Buildings"
                        GROUP BY "BuildingNumber"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION
                            'Doppelte BuildingNumber erkannt. Migration wird abgebrochen; Bestandsnummern werden nicht automatisch neu zugeordnet.';
                    END IF;
                END
                $$;

                SELECT setval(
                    '"BuildingNumberSequence"',
                    GREATEST(
                        COALESCE((
                            SELECT MAX(
                                substring(
                                    "BuildingNumber"
                                    FROM '^BLD-([0-9]+)$'
                                )::bigint
                            )
                            FROM "Buildings"
                            WHERE "BuildingNumber" ~ '^BLD-[0-9]+$'
                        ), 0) + 1,
                        1
                    ),
                    false
                );

                UPDATE "Buildings"
                SET "BuildingNumber" =
                    'BLD-' ||
                    LPAD(
                        nextval('"BuildingNumberSequence"')::text,
                        6,
                        '0'
                    )
                WHERE "BuildingNumber" IS NULL
                   OR BTRIM("BuildingNumber") = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "BuildingNumberSequence");
        }
    }
}
