using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateStatusReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CancellationReasonId",
                table: "TransferLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancellationReasonId",
                table: "InventoryCountingLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancellationReasonId",
                table: "GoodsReceiptLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferLines_CancellationReasonId",
                table: "TransferLines",
                column: "CancellationReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingLines_CancellationReasonId",
                table: "InventoryCountingLines",
                column: "CancellationReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_CancellationReasonId",
                table: "GoodsReceiptLines",
                column: "CancellationReasonId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptLines_CancellationReasons_CancellationReasonId",
                table: "GoodsReceiptLines",
                column: "CancellationReasonId",
                principalTable: "CancellationReasons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryCountingLines_CancellationReasons_CancellationReasonId",
                table: "InventoryCountingLines",
                column: "CancellationReasonId",
                principalTable: "CancellationReasons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransferLines_CancellationReasons_CancellationReasonId",
                table: "TransferLines",
                column: "CancellationReasonId",
                principalTable: "CancellationReasons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptLines_CancellationReasons_CancellationReasonId",
                table: "GoodsReceiptLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryCountingLines_CancellationReasons_CancellationReasonId",
                table: "InventoryCountingLines");

            migrationBuilder.DropForeignKey(
                name: "FK_TransferLines_CancellationReasons_CancellationReasonId",
                table: "TransferLines");

            migrationBuilder.DropIndex(
                name: "IX_TransferLines_CancellationReasonId",
                table: "TransferLines");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCountingLines_CancellationReasonId",
                table: "InventoryCountingLines");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptLines_CancellationReasonId",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "CancellationReasonId",
                table: "TransferLines");

            migrationBuilder.DropColumn(
                name: "CancellationReasonId",
                table: "InventoryCountingLines");

            migrationBuilder.DropColumn(
                name: "CancellationReasonId",
                table: "GoodsReceiptLines");
        }
    }
}
