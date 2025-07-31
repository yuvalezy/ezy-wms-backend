using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueKeyPickCheckItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PickListCheckItem_CheckSessionId_ItemCode",
                table: "PickListCheckItems");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckItem_CheckSessionId_ItemCode",
                table: "PickListCheckItems",
                columns: new[] { "CheckSessionId", "ItemCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PickListCheckItem_CheckSessionId_ItemCode",
                table: "PickListCheckItems");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckItem_CheckSessionId_ItemCode",
                table: "PickListCheckItems",
                columns: new[] { "CheckSessionId", "ItemCode" },
                unique: true);
        }
    }
}
