using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PickListCheckPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickListCheckPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBarcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickListCheckPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickListCheckPackages_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickListCheckPackages_PickListCheckSessions_CheckSessionId",
                        column: x => x.CheckSessionId,
                        principalTable: "PickListCheckSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PickListCheckPackages_Users_CheckedByUserId",
                        column: x => x.CheckedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickListCheckPackages_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickListCheckPackages_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckPackage_CheckedAt",
                table: "PickListCheckPackages",
                column: "CheckedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckPackage_CheckSessionId",
                table: "PickListCheckPackages",
                column: "CheckSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckPackage_CheckSessionId_PackageId",
                table: "PickListCheckPackages",
                columns: new[] { "CheckSessionId", "PackageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckPackage_PackageId",
                table: "PickListCheckPackages",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckPackages_CheckedByUserId",
                table: "PickListCheckPackages",
                column: "CheckedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckPackages_CreatedByUserId",
                table: "PickListCheckPackages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckPackages_UpdatedByUserId",
                table: "PickListCheckPackages",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickListCheckPackages");
        }
    }
}
