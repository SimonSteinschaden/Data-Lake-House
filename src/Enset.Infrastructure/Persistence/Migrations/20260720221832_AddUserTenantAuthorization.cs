using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enset.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTenantAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ImportReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "ImportReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalIdentity = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GlobalRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserCustomerAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCustomerAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCustomerAssignments_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCustomerAssignments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportReports_CreatedByUserId",
                table: "ImportReports",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReports_CustomerId",
                table: "ImportReports",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_ExternalIdentity",
                table: "ApplicationUsers",
                column: "ExternalIdentity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCustomerAssignments_CustomerId",
                table: "UserCustomerAssignments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCustomerAssignments_IsActive",
                table: "UserCustomerAssignments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserCustomerAssignments_Role",
                table: "UserCustomerAssignments",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_UserCustomerAssignments_UserId",
                table: "UserCustomerAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCustomerAssignments_UserId_CustomerId",
                table: "UserCustomerAssignments",
                columns: new[] { "UserId", "CustomerId" },
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCustomerAssignments");

            migrationBuilder.DropTable(
                name: "ApplicationUsers");

            migrationBuilder.DropIndex(
                name: "IX_ImportReports_CreatedByUserId",
                table: "ImportReports");

            migrationBuilder.DropIndex(
                name: "IX_ImportReports_CustomerId",
                table: "ImportReports");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ImportReports");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "ImportReports");
        }
    }
}
