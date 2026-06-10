using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PickingPackageLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PickingPackageLabelId",
                table: "PickLists",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScannedQuantity",
                table: "PickLists",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PickingPackageLabels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AbsEntry = table.Column<int>(type: "int", nullable: false),
                    WhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickingPackageLabels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickingPackageLabels_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickingPackageLabels_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickLists_PickingPackageLabelId",
                table: "PickLists",
                column: "PickingPackageLabelId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingPackageLabel_Unique_Code",
                table: "PickingPackageLabels",
                columns: new[] { "AbsEntry", "WhsCode", "Code" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PickingPackageLabel_Unique_Sequence",
                table: "PickingPackageLabels",
                columns: new[] { "AbsEntry", "WhsCode", "Sequence" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PickingPackageLabels_CreatedByUserId",
                table: "PickingPackageLabels",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingPackageLabels_UpdatedByUserId",
                table: "PickingPackageLabels",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickLists_PickingPackageLabels_PickingPackageLabelId",
                table: "PickLists",
                column: "PickingPackageLabelId",
                principalTable: "PickingPackageLabels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickLists_PickingPackageLabels_PickingPackageLabelId",
                table: "PickLists");

            migrationBuilder.DropTable(
                name: "PickingPackageLabels");

            migrationBuilder.DropIndex(
                name: "IX_PickLists_PickingPackageLabelId",
                table: "PickLists");

            migrationBuilder.DropColumn(
                name: "PickingPackageLabelId",
                table: "PickLists");

            migrationBuilder.DropColumn(
                name: "ScannedQuantity",
                table: "PickLists");
        }
    }
}
