using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductInventoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStocktakeSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StocktakeSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StocktakeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CountedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalItems = table.Column<int>(type: "int", nullable: false),
                    CountedItems = table.Column<int>(type: "int", nullable: false),
                    TotalVarianceQuantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StocktakeLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StocktakeSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SystemQuantity = table.Column<int>(type: "int", nullable: false),
                    CountedQuantity = table.Column<int>(type: "int", nullable: true),
                    VarianceQuantity = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StocktakeLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocktakeLines_StocktakeSessions_StocktakeSessionId",
                        column: x => x.StocktakeSessionId,
                        principalTable: "StocktakeSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeLines_ProductId",
                table: "StocktakeLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeLines_Session_Product",
                table: "StocktakeLines",
                columns: new[] { "StocktakeSessionId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_Status_StartedAt",
                table: "StocktakeSessions",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_StocktakeCode",
                table: "StocktakeSessions",
                column: "StocktakeCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StocktakeLines");

            migrationBuilder.DropTable(
                name: "StocktakeSessions");
        }
    }
}
