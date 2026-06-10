using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePickingPackageCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickListCheckPackages");

            migrationBuilder.DropTable(
                name: "PickListPackages");

            migrationBuilder.DropColumn(
                name: "TargetPackageId",
                table: "PackageCommitments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetPackageId",
                table: "PackageCommitments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PickListCheckPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PackageBarcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "PickListPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AbsEntry = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PickEntry = table.Column<int>(type: "int", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickListPackages", x => x.Id);
                    table.CheckConstraint("CK_PickListPackage_PickEntry_Required_For_Source", "([Type] != 0 OR [PickEntry] IS NOT NULL)");
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

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackage_AddedAt",
                table: "PickListPackages",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackage_Package",
                table: "PickListPackages",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackage_Type",
                table: "PickListPackages",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackage_Unique_Source",
                table: "PickListPackages",
                columns: new[] { "AbsEntry", "PickEntry", "PackageId", "Type" },
                unique: true,
                filter: "[PickEntry] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackage_Unique_Target",
                table: "PickListPackages",
                columns: new[] { "AbsEntry", "PackageId", "Type" },
                unique: true,
                filter: "[PickEntry] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackages_CreatedByUserId",
                table: "PickListPackages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListPackages_UpdatedByUserId",
                table: "PickListPackages",
                column: "UpdatedByUserId");
        }
    }
}
