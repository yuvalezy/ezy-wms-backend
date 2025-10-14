using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWmsAlertsAndWarehouseTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetWhsCode",
                table: "Transfers",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WmsAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertType = table.Column<int>(type: "int", nullable: false),
                    ObjectType = table.Column<int>(type: "int", nullable: false),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Data = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WmsAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WmsAlerts_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WmsAlerts_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WmsAlerts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WmsAlerts_CreatedAt",
                table: "WmsAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WmsAlerts_CreatedByUserId",
                table: "WmsAlerts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WmsAlerts_ObjectId",
                table: "WmsAlerts",
                column: "ObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WmsAlerts_UpdatedByUserId",
                table: "WmsAlerts",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WmsAlerts_UserId_IsRead",
                table: "WmsAlerts",
                columns: new[] { "UserId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WmsAlerts");

            migrationBuilder.DropColumn(
                name: "TargetWhsCode",
                table: "Transfers");
        }
    }
}
