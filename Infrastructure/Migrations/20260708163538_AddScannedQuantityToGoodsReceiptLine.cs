using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScannedQuantityToGoodsReceiptLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ScannedQuantity",
                table: "GoodsReceiptLines",
                type: "decimal(16,6)",
                precision: 16,
                scale: 6,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScannedQuantity",
                table: "GoodsReceiptLines");
        }
    }
}
