using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmationAdjustmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InventoryGoodsIssueAdjustmentEntry",
                table: "GoodsReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InventoryGoodsIssueAdjustmentExit",
                table: "GoodsReceipts",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InventoryGoodsIssueAdjustmentEntry",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "InventoryGoodsIssueAdjustmentExit",
                table: "GoodsReceipts");
        }
    }
}
