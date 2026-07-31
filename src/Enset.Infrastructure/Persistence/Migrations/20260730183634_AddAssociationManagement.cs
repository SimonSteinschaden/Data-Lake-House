using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations;

public partial class AddAssociationManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_EnergySystemBuildingAssignments_Buildings_BuildingId",
            table: "EnergySystemBuildingAssignments");
        migrationBuilder.DropForeignKey(
            name: "FK_EnergySystemBuildingAssignments_EnergySystems_EnergySystemId",
            table: "EnergySystemBuildingAssignments");
        migrationBuilder.Sql("""
            ALTER TABLE "EnergySystemBuildingAssignments"
            ALTER COLUMN "Role" TYPE character varying(40)
            USING CASE "Role"
              WHEN 1 THEN 'LocatedAt' WHEN 2 THEN 'Supplies'
              WHEN 3 THEN 'GeneratesFor' WHEN 4 THEN 'StoresEnergyFor'
              WHEN 5 THEN 'SharedSystem' WHEN 99 THEN 'Other'
              ELSE 'Unknown' END;
            """);
        migrationBuilder.AddColumn<bool>(
            name: "IsPrimary",
            table: "EnergySystemBuildingAssignments",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "AssociationAuditHistory",
            columns: table => new {
                Id=table.Column<Guid>("uuid",nullable:false),
                OperationId=table.Column<Guid>("uuid",nullable:false),
                ChangedAtUtc=table.Column<DateTime>("timestamp with time zone",nullable:false),
                ChangedByUserId=table.Column<Guid>("uuid",nullable:false),
                AssociationType=table.Column<string>("character varying(64)",maxLength:64,nullable:false),
                SourceId=table.Column<Guid>("uuid",nullable:false),
                TargetId=table.Column<Guid>("uuid",nullable:false),
                Action=table.Column<string>("character varying(32)",maxLength:32,nullable:false),
                Before=table.Column<string>("character varying(2000)",maxLength:2000,nullable:true),
                After=table.Column<string>("character varying(2000)",maxLength:2000,nullable:true),
                Reason=table.Column<string>("character varying(1000)",maxLength:1000,nullable:true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AssociationAuditHistory", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "BuildingMeterAssignments",
            columns: table => new {
                Id=table.Column<Guid>("uuid",nullable:false),BuildingId=table.Column<Guid>("uuid",nullable:false),
                MeterId=table.Column<Guid>("uuid",nullable:false),Role=table.Column<string>("character varying(40)",maxLength:40,nullable:false),
                IsPrimary=table.Column<bool>("boolean",nullable:false),ValidFrom=table.Column<DateOnly>("date",nullable:false),
                ValidTo=table.Column<DateOnly>("date",nullable:true),CreatedAt=table.Column<DateTime>("timestamp with time zone",nullable:false),
                UpdatedAt=table.Column<DateTime>("timestamp with time zone",nullable:true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BuildingMeterAssignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_BuildingMeterAssignments_Buildings_BuildingId",
                    column: x => x.BuildingId,
                    principalTable: "Buildings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_BuildingMeterAssignments_Meters_MeterId",
                    column: x => x.MeterId,
                    principalTable: "Meters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "BuildingDocumentAssignments",
            columns: table => new {
                Id=table.Column<Guid>("uuid",nullable:false),BuildingId=table.Column<Guid>("uuid",nullable:false),
                DocumentId=table.Column<Guid>("uuid",nullable:false),Role=table.Column<string>("character varying(40)",maxLength:40,nullable:false),
                ValidFrom=table.Column<DateOnly>("date",nullable:true),ValidTo=table.Column<DateOnly>("date",nullable:true),
                CreatedAt=table.Column<DateTime>("timestamp with time zone",nullable:false),UpdatedAt=table.Column<DateTime>("timestamp with time zone",nullable:true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BuildingDocumentAssignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_BuildingDocumentAssignments_Buildings_BuildingId",
                    column: x => x.BuildingId,
                    principalTable: "Buildings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_BuildingDocumentAssignments_Documents_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "Documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CustomerProjectAssignments",
            columns: table => new {
                Id=table.Column<Guid>("uuid",nullable:false),CustomerId=table.Column<Guid>("uuid",nullable:false),
                ProjectId=table.Column<Guid>("uuid",nullable:false),Role=table.Column<string>("character varying(40)",maxLength:40,nullable:false),
                IsPrimary=table.Column<bool>("boolean",nullable:false),ValidFrom=table.Column<DateOnly>("date",nullable:true),
                ValidTo=table.Column<DateOnly>("date",nullable:true),CreatedAt=table.Column<DateTime>("timestamp with time zone",nullable:false),
                UpdatedAt=table.Column<DateTime>("timestamp with time zone",nullable:true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerProjectAssignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_CustomerProjectAssignments_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CustomerProjectAssignments_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AssociationAuditHistory_OperationId",
            table: "AssociationAuditHistory",
            column: "OperationId");
        migrationBuilder.CreateIndex(
            name: "IX_AssociationAuditHistory_AssociationType_SourceId_TargetId_ChangedAtUtc",
            table: "AssociationAuditHistory",
            columns: new[] { "AssociationType", "SourceId", "TargetId", "ChangedAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_BuildingMeterAssignments_BuildingId_MeterId_ValidFrom",
            table: "BuildingMeterAssignments",
            columns: new[] { "BuildingId", "MeterId", "ValidFrom" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_BuildingMeterAssignments_MeterId_IsPrimary",
            table: "BuildingMeterAssignments",
            columns: new[] { "MeterId", "IsPrimary" });
        migrationBuilder.CreateIndex(
            name: "IX_BuildingMeterAssignments_ValidFrom_ValidTo",
            table: "BuildingMeterAssignments",
            columns: new[] { "ValidFrom", "ValidTo" });
        migrationBuilder.CreateIndex(
            name: "IX_BuildingDocumentAssignments_BuildingId_DocumentId_ValidFrom",
            table: "BuildingDocumentAssignments",
            columns: new[] { "BuildingId", "DocumentId", "ValidFrom" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_BuildingDocumentAssignments_DocumentId",
            table: "BuildingDocumentAssignments",
            column: "DocumentId");
        migrationBuilder.CreateIndex(
            name: "IX_CustomerProjectAssignments_CustomerId_ProjectId_ValidFrom",
            table: "CustomerProjectAssignments",
            columns: new[] { "CustomerId", "ProjectId", "ValidFrom" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_CustomerProjectAssignments_ProjectId_IsPrimary",
            table: "CustomerProjectAssignments",
            columns: new[] { "ProjectId", "IsPrimary" });
        migrationBuilder.CreateIndex(
            name: "IX_EnergySystemBuildingAssignments_EnergySystemId_IsPrimary",
            table: "EnergySystemBuildingAssignments",
            columns: new[] { "EnergySystemId", "IsPrimary" });
        migrationBuilder.CreateIndex(
            name: "IX_EnergySystemBuildingAssignments_BuildingId_EnergySystemId_ValidFrom",
            table: "EnergySystemBuildingAssignments",
            columns: new[] { "BuildingId", "EnergySystemId", "ValidFrom" },
            unique: true);
        migrationBuilder.AddForeignKey(
            name: "FK_EnergySystemBuildingAssignments_Buildings_BuildingId",
            table: "EnergySystemBuildingAssignments",
            column: "BuildingId",
            principalTable: "Buildings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_EnergySystemBuildingAssignments_EnergySystems_EnergySystemId",
            table: "EnergySystemBuildingAssignments",
            column: "EnergySystemId",
            principalTable: "EnergySystems",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql("""
            INSERT INTO "BuildingMeterAssignments" ("Id","BuildingId","MeterId","Role","IsPrimary","ValidFrom","CreatedAt")
            SELECT gen_random_uuid(), "BuildingId", "Id", 'MainMeter', true, CURRENT_DATE, CURRENT_TIMESTAMP
            FROM "Meters" WHERE "BuildingId" IS NOT NULL;
            INSERT INTO "CustomerProjectAssignments" ("Id","CustomerId","ProjectId","Role","IsPrimary","CreatedAt")
            SELECT gen_random_uuid(), "CustomerId", "Id", 'Client', true, CURRENT_TIMESTAMP FROM "Projects";
            CREATE UNIQUE INDEX "UX_BuildingMeterAssignments_ActivePrimary"
              ON "BuildingMeterAssignments" ("MeterId")
              WHERE "IsPrimary" AND "ValidTo" IS NULL;
            CREATE UNIQUE INDEX "UX_CustomerProjectAssignments_ActivePrimary"
              ON "CustomerProjectAssignments" ("ProjectId")
              WHERE "IsPrimary" AND "ValidTo" IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_EnergySystemBuildingAssignments_Buildings_BuildingId",
            table: "EnergySystemBuildingAssignments");
        migrationBuilder.DropForeignKey(
            name: "FK_EnergySystemBuildingAssignments_EnergySystems_EnergySystemId",
            table: "EnergySystemBuildingAssignments");
        migrationBuilder.DropTable(
            name: "AssociationAuditHistory");
        migrationBuilder.DropTable(
            name: "BuildingDocumentAssignments");
        migrationBuilder.DropTable(
            name: "BuildingMeterAssignments");
        migrationBuilder.DropTable(
            name: "CustomerProjectAssignments");
        migrationBuilder.DropIndex(
            name: "IX_EnergySystemBuildingAssignments_EnergySystemId_IsPrimary",
            table: "EnergySystemBuildingAssignments");
        migrationBuilder.DropIndex(
            name: "IX_EnergySystemBuildingAssignments_BuildingId_EnergySystemId_ValidFrom",
            table: "EnergySystemBuildingAssignments");
        migrationBuilder.DropColumn(
            name: "IsPrimary",
            table: "EnergySystemBuildingAssignments");
        migrationBuilder.Sql("""
            ALTER TABLE "EnergySystemBuildingAssignments"
            ALTER COLUMN "Role" TYPE integer
            USING CASE "Role"
              WHEN 'LocatedAt' THEN 1 WHEN 'Supplies' THEN 2
              WHEN 'GeneratesFor' THEN 3 WHEN 'StoresEnergyFor' THEN 4
              WHEN 'SharedSystem' THEN 5 WHEN 'Other' THEN 99
              ELSE 0 END;
            """);
        migrationBuilder.AddForeignKey(
            name: "FK_EnergySystemBuildingAssignments_Buildings_BuildingId",
            table: "EnergySystemBuildingAssignments",
            column: "BuildingId",
            principalTable: "Buildings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey(
            name: "FK_EnergySystemBuildingAssignments_EnergySystems_EnergySystemId",
            table: "EnergySystemBuildingAssignments",
            column: "EnergySystemId",
            principalTable: "EnergySystems",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
