using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePackageCapability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryCountingPackageContents");

            migrationBuilder.DropTable(
                name: "PackageCommitments");

            migrationBuilder.DropTable(
                name: "PackageContents");

            migrationBuilder.DropTable(
                name: "PackageInconsistencies");

            migrationBuilder.DropTable(
                name: "PackageLocationHistory");

            migrationBuilder.DropTable(
                name: "PackageTransactions");

            migrationBuilder.DropTable(
                name: "TransferPackages");

            migrationBuilder.DropTable(
                name: "InventoryCountingPackages");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropColumn(
                name: "PackageId",
                table: "InventoryCountingLines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PackageId",
                table: "InventoryCountingLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uniqueidentifier", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CustomAttributes = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceOperationType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packages_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Packages_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferPackages_Transfers_TransferId",
                        column: x => x.TransferId,
                        principalTable: "Transfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransferPackages_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferPackages_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCountingPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InventoryCountingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CountedBinEntry = table.Column<int>(type: "int", nullable: true),
                    CountedWhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsNewPackage = table.Column<bool>(type: "bit", nullable: false),
                    OriginalBinEntry = table.Column<int>(type: "int", nullable: true),
                    OriginalStatus = table.Column<int>(type: "int", nullable: false),
                    OriginalWhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PackageBarcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCountingPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackages_InventoryCountings_InventoryCountingId",
                        column: x => x.InventoryCountingId,
                        principalTable: "InventoryCountings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackages_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackages_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackages_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageCommitments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SourceOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceOperationLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceOperationType = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageCommitments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageCommitments_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageCommitments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageCommitments_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    CommittedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageContents_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageContents_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageContents_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageInconsistencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BatchNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InconsistencyType = table.Column<int>(type: "int", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PackageBarcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PackageQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ResolutionAction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SapQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    SerialNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WmsQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageInconsistencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageInconsistencies_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageInconsistencies_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageInconsistencies_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageLocationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FromBinEntry = table.Column<int>(type: "int", nullable: true),
                    FromWhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceOperationType = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    ToBinEntry = table.Column<int>(type: "int", nullable: true),
                    ToWhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageLocationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageLocationHistory_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageLocationHistory_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageLocationHistory_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SourceOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceOperationLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceOperationType = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    UnitQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitType = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageTransactions_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageTransactions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageTransactions_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCountingPackageContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InventoryCountingPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCountingPackageContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackageContents_InventoryCountingPackages_InventoryCountingPackageId",
                        column: x => x.InventoryCountingPackageId,
                        principalTable: "InventoryCountingPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackageContents_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCountingPackageContents_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackageContents_CreatedByUserId",
                table: "InventoryCountingPackageContents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackageContents_InventoryCountingPackageId_ItemCode",
                table: "InventoryCountingPackageContents",
                columns: new[] { "InventoryCountingPackageId", "ItemCode" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackageContents_UpdatedByUserId",
                table: "InventoryCountingPackageContents",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackages_CreatedByUserId",
                table: "InventoryCountingPackages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackages_InventoryCountingId_PackageId",
                table: "InventoryCountingPackages",
                columns: new[] { "InventoryCountingId", "PackageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackages_PackageId",
                table: "InventoryCountingPackages",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingPackages_UpdatedByUserId",
                table: "InventoryCountingPackages",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageCommitment_Date",
                table: "PackageCommitments",
                column: "CommittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PackageCommitment_Item",
                table: "PackageCommitments",
                column: "ItemCode");

            migrationBuilder.CreateIndex(
                name: "IX_PackageCommitment_Operation",
                table: "PackageCommitments",
                columns: new[] { "SourceOperationType", "SourceOperationId" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageCommitment_Package",
                table: "PackageCommitments",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageCommitment_PackageItem",
                table: "PackageCommitments",
                columns: new[] { "PackageId", "ItemCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageCommitments_CreatedByUserId",
                table: "PackageCommitments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageCommitments_UpdatedByUserId",
                table: "PackageCommitments",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageContent_Item",
                table: "PackageContents",
                column: "ItemCode");

            migrationBuilder.CreateIndex(
                name: "IX_PackageContent_Location",
                table: "PackageContents",
                columns: new[] { "WhsCode", "BinEntry" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageContent_Package",
                table: "PackageContents",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageContents_CreatedByUserId",
                table: "PackageContents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageContents_UpdatedByUserId",
                table: "PackageContents",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageInconsistencies_CreatedByUserId",
                table: "PackageInconsistencies",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageInconsistencies_UpdatedByUserId",
                table: "PackageInconsistencies",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageInconsistency_DetectedAt",
                table: "PackageInconsistencies",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PackageInconsistency_IsResolved",
                table: "PackageInconsistencies",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_PackageInconsistency_Package",
                table: "PackageInconsistencies",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageInconsistency_TypeSeverity",
                table: "PackageInconsistencies",
                columns: new[] { "InconsistencyType", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageLocationHistory_CreatedByUserId",
                table: "PackageLocationHistory",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageLocationHistory_Date",
                table: "PackageLocationHistory",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_PackageLocationHistory_Package",
                table: "PackageLocationHistory",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageLocationHistory_UpdatedByUserId",
                table: "PackageLocationHistory",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Package_Barcode",
                table: "Packages",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Package_CreatedAt",
                table: "Packages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Package_Location",
                table: "Packages",
                columns: new[] { "WhsCode", "BinEntry" });

            migrationBuilder.CreateIndex(
                name: "IX_Package_Status",
                table: "Packages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_CreatedByUserId",
                table: "Packages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_UpdatedByUserId",
                table: "Packages",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageTransaction_Date",
                table: "PackageTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_PackageTransaction_Operation",
                table: "PackageTransactions",
                columns: new[] { "SourceOperationType", "SourceOperationId" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageTransaction_Package",
                table: "PackageTransactions",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageTransactions_CreatedByUserId",
                table: "PackageTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageTransactions_UpdatedByUserId",
                table: "PackageTransactions",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferPackages_CreatedByUserId",
                table: "TransferPackages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferPackages_TransferId_PackageId_Type",
                table: "TransferPackages",
                columns: new[] { "TransferId", "PackageId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferPackages_UpdatedByUserId",
                table: "TransferPackages",
                column: "UpdatedByUserId");
        }
    }
}
