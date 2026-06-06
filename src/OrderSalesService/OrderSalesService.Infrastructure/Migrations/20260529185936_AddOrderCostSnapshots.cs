using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSalesService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCostSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostTotal",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "CostTotal",
                table: "OrderDetails");
        }
    }
}
