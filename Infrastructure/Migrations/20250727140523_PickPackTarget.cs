using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PickPackTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PickListPackages",
                table: "PickListPackages");

            migrationBuilder.AlterColumn<int>(
                name: "PickEntry",
                table: "PickListPackages",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PickListPackages",
                table: "PickListPackages",
                column: "Id");

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_PickListPackage_PickEntry_Required_For_Source",
                table: "PickListPackages",
                sql: "([Type] != 0 OR [PickEntry] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PickListPackages",
                table: "PickListPackages");

            migrationBuilder.DropIndex(
                name: "IX_PickListPackage_Unique_Source",
                table: "PickListPackages");

            migrationBuilder.DropIndex(
                name: "IX_PickListPackage_Unique_Target",
                table: "PickListPackages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PickListPackage_PickEntry_Required_For_Source",
                table: "PickListPackages");

            migrationBuilder.AlterColumn<int>(
                name: "PickEntry",
                table: "PickListPackages",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PickListPackages",
                table: "PickListPackages",
                columns: new[] { "AbsEntry", "PickEntry", "PackageId", "Type" });
        }
    }
}
