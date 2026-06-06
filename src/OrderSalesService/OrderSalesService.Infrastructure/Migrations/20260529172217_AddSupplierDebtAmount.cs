using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSalesService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierDebtAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmount",
                table: "Suppliers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebtAmount",
                table: "Suppliers");
        }
    }
}
