using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCrudApiAuditAndReadingIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MeterReadings",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_MeterId_Timestamp",
                table: "MeterReadings");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Meters",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Meters",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "EnergySystems",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "EnergySystems",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Customers",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Customers",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Buildings",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Buildings",
                newName: "CreatedAtUtc");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Meters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataOrigin",
                table: "Meters",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Imported");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Meters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Meters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Meters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastImportId",
                table: "Meters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedSource",
                table: "Meters",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Import");

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Meters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Meters",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "MeterReadings",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "MeterReadings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "MeterReadings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataOrigin",
                table: "MeterReadings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Imported");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "MeterReadings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "MeterReadings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MeterReadings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastImportId",
                table: "MeterReadings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedSource",
                table: "MeterReadings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Import");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "MeterReadings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "MeterReadings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "MeterReadings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "CommissionedAt",
                table: "EnergySystems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "EnergySystems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataOrigin",
                table: "EnergySystems",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Imported");

            migrationBuilder.AddColumn<DateTime>(
                name: "DecommissionedAt",
                table: "EnergySystems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "EnergySystems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "EnergySystems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EnergySystems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastImportId",
                table: "EnergySystems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedSource",
                table: "EnergySystems",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Import");

            migrationBuilder.AddColumn<decimal>(
                name: "RatedPowerKw",
                table: "EnergySystems",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "EnergySystems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "EnergySystems",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataOrigin",
                table: "Customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Imported");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastImportId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedSource",
                table: "Customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Import");

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Customers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Buildings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataOrigin",
                table: "Buildings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Imported");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Buildings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Buildings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Buildings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastImportId",
                table: "Buildings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedSource",
                table: "Buildings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Import");

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Buildings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Buildings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MeterReadings",
                table: "MeterReadings",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "EntityAuditHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OldValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityAuditHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Meters_DataOrigin",
                table: "Meters",
                column: "DataOrigin");

            migrationBuilder.CreateIndex(
                name: "IX_Meters_IsDeleted",
                table: "Meters",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Meters_UpdatedAtUtc",
                table: "Meters",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_DataOrigin",
                table: "MeterReadings",
                column: "DataOrigin");

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_IsDeleted",
                table: "MeterReadings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_MeterId_Timestamp",
                table: "MeterReadings",
                columns: new[] { "MeterId", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_UpdatedAtUtc",
                table: "MeterReadings",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EnergySystems_DataOrigin",
                table: "EnergySystems",
                column: "DataOrigin");

            migrationBuilder.CreateIndex(
                name: "IX_EnergySystems_IsDeleted",
                table: "EnergySystems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EnergySystems_UpdatedAtUtc",
                table: "EnergySystems",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_DataOrigin",
                table: "Customers",
                column: "DataOrigin");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsDeleted",
                table: "Customers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UpdatedAtUtc",
                table: "Customers",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_DataOrigin",
                table: "Buildings",
                column: "DataOrigin");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_IsDeleted",
                table: "Buildings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_UpdatedAtUtc",
                table: "Buildings",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditHistory_ChangedByUserId",
                table: "EntityAuditHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditHistory_EntityType_EntityId_ChangedAtUtc",
                table: "EntityAuditHistory",
                columns: new[] { "EntityType", "EntityId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityAuditHistory");

            migrationBuilder.DropIndex(
                name: "IX_Meters_DataOrigin",
                table: "Meters");

            migrationBuilder.DropIndex(
                name: "IX_Meters_IsDeleted",
                table: "Meters");

            migrationBuilder.DropIndex(
                name: "IX_Meters_UpdatedAtUtc",
                table: "Meters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MeterReadings",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_DataOrigin",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_IsDeleted",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_MeterId_Timestamp",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_UpdatedAtUtc",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_EnergySystems_DataOrigin",
                table: "EnergySystems");

            migrationBuilder.DropIndex(
                name: "IX_EnergySystems_IsDeleted",
                table: "EnergySystems");

            migrationBuilder.DropIndex(
                name: "IX_EnergySystems_UpdatedAtUtc",
                table: "EnergySystems");

            migrationBuilder.DropIndex(
                name: "IX_Customers_DataOrigin",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_IsDeleted",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_UpdatedAtUtc",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_DataOrigin",
                table: "Buildings");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_IsDeleted",
                table: "Buildings");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_UpdatedAtUtc",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "DataOrigin",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "LastImportId",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "LastModifiedSource",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "DataOrigin",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "LastImportId",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "LastModifiedSource",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "CommissionedAt",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "DataOrigin",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "DecommissionedAt",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "LastImportId",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "LastModifiedSource",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "RatedPowerKw",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "EnergySystems");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DataOrigin",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastImportId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastModifiedSource",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "DataOrigin",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "LastImportId",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "LastModifiedSource",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Buildings");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Meters",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Meters",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "EnergySystems",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "EnergySystems",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Customers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Customers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Buildings",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Buildings",
                newName: "CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MeterReadings",
                table: "MeterReadings",
                columns: new[] { "MeterId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_MeterId_Timestamp",
                table: "MeterReadings",
                columns: new[] { "MeterId", "Timestamp" });
        }
    }
}
