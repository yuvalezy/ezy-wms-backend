using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthorizationGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Authorizations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SuperUser = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Warehouses = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AuthorizationGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_AuthorizationGroups_AuthorizationGroupId",
                        column: x => x.AuthorizationGroupId,
                        principalTable: "AuthorizationGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CardCode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    WhsCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCountings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvCountEntry = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WhsCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCountings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCountings_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCountings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", maxLength: 50, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uniqueidentifier", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomAttributes = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "PickLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AbsEntry = table.Column<int>(type: "int", nullable: false),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PickEntry = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SyncStatus = table.Column<int>(type: "int", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SyncError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickLists_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickLists_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WhsCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transfers_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfers_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocEntry = table.Column<int>(type: "int", nullable: false),
                    ObjType = table.Column<int>(type: "int", nullable: false),
                    DocNumber = table.Column<int>(type: "int", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptDocuments_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptDocuments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptDocuments_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BarCode = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LineStatus = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(16,6)", precision: 16, scale: 6, nullable: false),
                    StatusReason = table.Column<int>(type: "int", nullable: true),
                    CancellationReasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_CancellationReasons_CancellationReasonId",
                        column: x => x.CancellationReasonId,
                        principalTable: "CancellationReasons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCountingLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BarCode = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LineStatus = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    StatusReason = table.Column<int>(type: "int", nullable: true),
                    CancellationReasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    InventoryCountingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCountingLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCountingLines_CancellationReasons_CancellationReasonId",
                        column: x => x.CancellationReasonId,
                        principalTable: "CancellationReasons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryCountingLines_InventoryCountings_InventoryCountingId",
                        column: x => x.InventoryCountingId,
                        principalTable: "InventoryCountings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryCountingLines_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCountingLines_Users_UpdatedByUserId",
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
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: false),
                    WhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBarcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BatchNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SerialNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    SapQuantity = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: true),
                    WmsQuantity = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: true),
                    PackageQuantity = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: true),
                    InconsistencyType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResolutionAction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    FromWhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FromBinEntry = table.Column<int>(type: "int", nullable: true),
                    ToWhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ToBinEntry = table.Column<int>(type: "int", nullable: true),
                    SourceOperationType = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    SourceOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 50, nullable: false),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitQuantity = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitType = table.Column<int>(type: "int", nullable: false),
                    SourceOperationType = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    SourceOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceOperationLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 50, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "TransferLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BarCode = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    BinEntry = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LineStatus = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    StatusReason = table.Column<int>(type: "int", nullable: true),
                    CancellationReasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    UnitType = table.Column<int>(type: "int", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferLines_CancellationReasons_CancellationReasonId",
                        column: x => x.CancellationReasonId,
                        principalTable: "CancellationReasons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferLines_Transfers_TransferId",
                        column: x => x.TransferId,
                        principalTable: "Transfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransferLines_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferLines_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(16,6)", precision: 16, scale: 6, nullable: false),
                    SourceEntry = table.Column<int>(type: "int", nullable: false),
                    SourceNumber = table.Column<int>(type: "int", nullable: false),
                    SourceLine = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptSources_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptSources_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptSources_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WhsCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    TargetEntry = table.Column<int>(type: "int", nullable: false),
                    TargetLine = table.Column<int>(type: "int", nullable: false),
                    TargetQuantity = table.Column<decimal>(type: "decimal(16,6)", precision: 16, scale: 6, nullable: false),
                    TargetStatus = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptTargets_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptTargets_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptTargets_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationGroups_CreatedByUserId",
                table: "AuthorizationGroups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationGroups_UpdatedByUserId",
                table: "AuthorizationGroups",
                column: "UpdatedByUserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptDocuments_CreatedByUserId",
                table: "GoodsReceiptDocuments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptDocuments_GoodsReceiptId",
                table: "GoodsReceiptDocuments",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptDocuments_UpdatedByUserId",
                table: "GoodsReceiptDocuments",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_CancellationReasonId",
                table: "GoodsReceiptLines",
                column: "CancellationReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_CreatedByUserId",
                table: "GoodsReceiptLines",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_GoodsReceiptId",
                table: "GoodsReceiptLines",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_UpdatedByUserId",
                table: "GoodsReceiptLines",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_CreatedByUserId",
                table: "GoodsReceipts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_UpdatedByUserId",
                table: "GoodsReceipts",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptSources_CreatedByUserId",
                table: "GoodsReceiptSources",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptSources_GoodsReceiptLineId",
                table: "GoodsReceiptSources",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptSources_UpdatedByUserId",
                table: "GoodsReceiptSources",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptTargets_CreatedByUserId",
                table: "GoodsReceiptTargets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptTargets_GoodsReceiptLineId",
                table: "GoodsReceiptTargets",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptTargets_UpdatedByUserId",
                table: "GoodsReceiptTargets",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingLines_CancellationReasonId",
                table: "InventoryCountingLines",
                column: "CancellationReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingLines_CreatedByUserId",
                table: "InventoryCountingLines",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingLines_InventoryCountingId",
                table: "InventoryCountingLines",
                column: "InventoryCountingId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountingLines_UpdatedByUserId",
                table: "InventoryCountingLines",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountings_CreatedByUserId",
                table: "InventoryCountings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountings_UpdatedByUserId",
                table: "InventoryCountings",
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
                name: "IX_PickLists_CreatedByUserId",
                table: "PickLists",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PickLists_UpdatedByUserId",
                table: "PickLists",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferLines_CancellationReasonId",
                table: "TransferLines",
                column: "CancellationReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferLines_CreatedByUserId",
                table: "TransferLines",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferLines_TransferId",
                table: "TransferLines",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferLines_UpdatedByUserId",
                table: "TransferLines",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_CreatedByUserId",
                table: "Transfers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_UpdatedByUserId",
                table: "Transfers",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AuthorizationGroupId",
                table: "Users",
                column: "AuthorizationGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationGroups_Users_CreatedByUserId",
                table: "AuthorizationGroups",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationGroups_Users_UpdatedByUserId",
                table: "AuthorizationGroups",
                column: "UpdatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroups_Users_CreatedByUserId",
                table: "AuthorizationGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroups_Users_UpdatedByUserId",
                table: "AuthorizationGroups");

            migrationBuilder.DropTable(
                name: "GoodsReceiptDocuments");

            migrationBuilder.DropTable(
                name: "GoodsReceiptSources");

            migrationBuilder.DropTable(
                name: "GoodsReceiptTargets");

            migrationBuilder.DropTable(
                name: "InventoryCountingLines");

            migrationBuilder.DropTable(
                name: "PackageContents");

            migrationBuilder.DropTable(
                name: "PackageInconsistencies");

            migrationBuilder.DropTable(
                name: "PackageLocationHistory");

            migrationBuilder.DropTable(
                name: "PackageTransactions");

            migrationBuilder.DropTable(
                name: "PickLists");

            migrationBuilder.DropTable(
                name: "TransferLines");

            migrationBuilder.DropTable(
                name: "GoodsReceiptLines");

            migrationBuilder.DropTable(
                name: "InventoryCountings");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropTable(
                name: "Transfers");

            migrationBuilder.DropTable(
                name: "CancellationReasons");

            migrationBuilder.DropTable(
                name: "GoodsReceipts");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "AuthorizationGroups");
        }
    }
}
