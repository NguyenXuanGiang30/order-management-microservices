using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSalesService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Variance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashShifts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_Cashier_Status",
                table: "CashShifts",
                columns: new[] { "CashierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_ShiftCode",
                table: "CashShifts",
                column: "ShiftCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashShifts");
        }
    }
}
