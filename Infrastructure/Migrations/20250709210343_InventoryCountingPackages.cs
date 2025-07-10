using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryCountingPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryCountingPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryCountingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBarcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalWhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalBinEntry = table.Column<int>(type: "int", nullable: true),
                    CountedWhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CountedBinEntry = table.Column<int>(type: "int", nullable: true),
                    IsNewPackage = table.Column<bool>(type: "bit", nullable: false),
                    OriginalStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCountingPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackages_InventoryCountings_InventoryCountingId",
                        column: x => x.InventoryCountingId,
                        principalTable: "InventoryCountings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackages_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackages_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackages_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCountingPackageContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryCountingPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: true),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCountingPackageContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackageContents_InventoryCountingPackages_InventoryCountingPackageId",
                        column: x => x.InventoryCountingPackageId,
                        principalTable: "InventoryCountingPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackageContents_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackageContents_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackageContents_CreatedByUserId",
                table: "InventoryCountingPackageContents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackageContents_InventoryCountingPackageId_ItemCode",
                table: "InventoryCountingPackageContents",
                columns: new[] { "InventoryCountingPackageId", "ItemCode" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackageContents_UpdatedByUserId",
                table: "InventoryCountingPackageContents",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackages_CreatedByUserId",
                table: "InventoryCountingPackages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackages_InventoryCountingId_PackageId",
                table: "InventoryCountingPackages",
                columns: new[] { "InventoryCountingId", "PackageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackages_PackageId",
                table: "InventoryCountingPackages",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackages_UpdatedByUserId",
                table: "InventoryCountingPackages",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryCountingPackageContents");

            migrationBuilder.DropTable(
                name: "InventoryCountingPackages");
        }
    }
}
