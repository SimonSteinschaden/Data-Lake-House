using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDataProductVerticalSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProductDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResultType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProductDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProductGenerationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GeneratorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    GeneratorVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InputHash = table.Column<string>(type: "text", nullable: true),
                    ParameterJson = table.Column<string>(type: "text", nullable: true),
                    Warnings = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    TriggeredBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProductGenerationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProductDefinitionScope",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataProductDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProductDefinitionScope", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProductDefinitionScope_DataProductDefinitions_DataProdu~",
                        column: x => x.DataProductDefinitionId,
                        principalTable: "DataProductDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProducts_DataProductDefinitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "DataProductDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataProductCustomerAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProductCustomerAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProductCustomerAssignment_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataProductCustomerAssignment_DataProducts_DataProductId",
                        column: x => x.DataProductId,
                        principalTable: "DataProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataProductScopeAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnergySystemId = table.Column<Guid>(type: "uuid", nullable: true),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: true),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnergyCommunityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProductScopeAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProductScopeAssignments_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataProductScopeAssignments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataProductScopeAssignments_DataProducts_DataProductId",
                        column: x => x.DataProductId,
                        principalTable: "DataProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataProductScopeAssignments_EnergyCommunities_EnergyCommuni~",
                        column: x => x.EnergyCommunityId,
                        principalTable: "EnergyCommunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataProductScopeAssignments_EnergySystems_EnergySystemId",
                        column: x => x.EnergySystemId,
                        principalTable: "EnergySystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataProductScopeAssignments_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataProductScopeAssignments_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataProductScopeAssignments_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataProductVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InputPeriodFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InputPeriodTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quality = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GenerationRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProductVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProductVersions_DataProductGenerationRuns_GenerationRun~",
                        column: x => x.GenerationRunId,
                        principalTable: "DataProductGenerationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DataProductVersions_DataProducts_DataProductId",
                        column: x => x.DataProductId,
                        principalTable: "DataProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataProductValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataProductVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NumericValue = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    TextValue = table.Column<string>(type: "text", nullable: true),
                    BooleanValue = table.Column<bool>(type: "boolean", nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Quality = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProductValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProductValues_DataProductVersions_DataProductVersionId",
                        column: x => x.DataProductVersionId,
                        principalTable: "DataProductVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataProductCustomerAssignment_CustomerId",
                table: "DataProductCustomerAssignment",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProductCustomerAssignment_DataProductId",
                table: "DataProductCustomerAssignment",
                column: "DataProductId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProductDefinitions_Code",
                table: "DataProductDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataProductDefinitionScope_DataProductDefinitionId",
                table: "DataProductDefinitionScope",
                column: "DataProductDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProducts_DefinitionId",
                table: "DataProducts",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProducts_ProductNumber",
                table: "DataProducts",
                column: "ProductNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataProductScopeAssignments_BuildingId",
                table: "DataProductScopeAssignments",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProductScopeAssignments_CustomerId",
                table: "DataProductScopeAssignments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProductScopeAssignments_DataProductId",
                table: "DataProductScopeAssignments",
                column: "DataProductId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProductScopeAssignments_EnergyCommunityId",
                table: "DataProductScopeAssignments",
                column: "EnergyCommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProductScopeAssignments_EnergySystemId",
                table: "DataProductScopeAssignments",
                column: "EnergySystemId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProductScopeAssignments_MeterId",
                table: "DataProductScopeAssignments",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProductScopeAssignments_MunicipalityId",
                table: "DataProductScopeAssignments",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProductScopeAssignments_RegionId",
                table: "DataProductScopeAssignments",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProductValues_DataProductVersionId",
                table: "DataProductValues",
                column: "DataProductVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProductVersions_DataProductId_VersionNumber",
                table: "DataProductVersions",
                columns: new[] { "DataProductId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataProductVersions_GenerationRunId",
                table: "DataProductVersions",
                column: "GenerationRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProductCustomerAssignment");

            migrationBuilder.DropTable(
                name: "DataProductDefinitionScope");

            migrationBuilder.DropTable(
                name: "DataProductScopeAssignments");

            migrationBuilder.DropTable(
                name: "DataProductValues");

            migrationBuilder.DropTable(
                name: "DataProductVersions");

            migrationBuilder.DropTable(
                name: "DataProductGenerationRuns");

            migrationBuilder.DropTable(
                name: "DataProducts");

            migrationBuilder.DropTable(
                name: "DataProductDefinitions");
        }
    }
}
