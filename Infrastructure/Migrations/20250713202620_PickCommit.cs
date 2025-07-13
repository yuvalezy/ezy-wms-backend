using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PickCommit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickListPackages",
                columns: table => new
                {
                    AbsEntry = table.Column<int>(type: "int", nullable: false),
                    PickEntry = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickListPackages", x => new { x.AbsEntry, x.PickEntry, x.PackageId, x.Type });
                    table.ForeignKey(
                        name: "FK_PickListPackages_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickListPackages_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickListPackages_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackage_AddedAt",
                table: "PickListPackages",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackage_Operation",
                table: "PickListPackages",
                columns: new[] { "AbsEntry", "PickEntry" });

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackage_Package",
                table: "PickListPackages",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackage_Type",
                table: "PickListPackages",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackages_CreatedByUserId",
                table: "PickListPackages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackages_UpdatedByUserId",
                table: "PickListPackages",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickListPackages");
        }
    }
}
