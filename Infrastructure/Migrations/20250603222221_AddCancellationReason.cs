using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CancellationReasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Transfer = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    GoodsReceipt = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Counting = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationReasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancellationReasons_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CancellationReasons_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationReasons_CreatedByUserId",
                table: "CancellationReasons",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationReasons_ObjectTypes",
                table: "CancellationReasons",
                columns: new[] { "Transfer", "GoodsReceipt", "Counting", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationReasons_UpdatedByUserId",
                table: "CancellationReasons",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CancellationReasons");
        }
    }
}
