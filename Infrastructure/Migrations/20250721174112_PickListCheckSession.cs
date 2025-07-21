using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PickListCheckSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickListCheckSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PickListId = table.Column<int>(type: "int", nullable: false),
                    StartedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedByUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickListCheckSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickListCheckSessions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickListCheckSessions_Users_StartedByUserId",
                        column: x => x.StartedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickListCheckSessions_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PickListCheckItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CheckedQuantity = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickListCheckItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickListCheckItems_PickListCheckSessions_CheckSessionId",
                        column: x => x.CheckSessionId,
                        principalTable: "PickListCheckSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PickListCheckItems_Users_CheckedByUserId",
                        column: x => x.CheckedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickListCheckItems_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickListCheckItems_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckItem_CheckedAt",
                table: "PickListCheckItems",
                column: "CheckedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckItem_CheckSessionId",
                table: "PickListCheckItems",
                column: "CheckSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckItem_CheckSessionId_ItemCode",
                table: "PickListCheckItems",
                columns: new[] { "CheckSessionId", "ItemCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckItem_ItemCode",
                table: "PickListCheckItems",
                column: "ItemCode");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckItems_CheckedByUserId",
                table: "PickListCheckItems",
                column: "CheckedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckItems_CreatedByUserId",
                table: "PickListCheckItems",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckItems_UpdatedByUserId",
                table: "PickListCheckItems",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckSession_IsCompleted",
                table: "PickListCheckSessions",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckSession_PickListId",
                table: "PickListCheckSessions",
                column: "PickListId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckSession_PickListId_IsCompleted",
                table: "PickListCheckSessions",
                columns: new[] { "PickListId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckSession_StartedAt",
                table: "PickListCheckSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckSessions_CreatedByUserId",
                table: "PickListCheckSessions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckSessions_StartedByUserId",
                table: "PickListCheckSessions",
                column: "StartedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListCheckSessions_UpdatedByUserId",
                table: "PickListCheckSessions",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickListCheckItems");

            migrationBuilder.DropTable(
                name: "PickListCheckSessions");
        }
    }
}
