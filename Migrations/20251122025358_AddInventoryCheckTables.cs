using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UiDesktopApp1.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCheckTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCheckDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryCheckId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SystemQty = table.Column<int>(type: "int", nullable: false),
                    ActualQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCheckDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCheckDetails_InventoryChecks_InventoryCheckId",
                        column: x => x.InventoryCheckId,
                        principalTable: "InventoryChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryCheckDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCheckDetails_InventoryCheckId",
                table: "InventoryCheckDetails",
                column: "InventoryCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCheckDetails_ProductId",
                table: "InventoryCheckDetails",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryCheckDetails");

            migrationBuilder.DropTable(
                name: "InventoryChecks");
        }
    }
}
