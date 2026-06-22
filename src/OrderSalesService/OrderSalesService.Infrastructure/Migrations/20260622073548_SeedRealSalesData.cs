using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderSalesService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRealSalesData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Address", "Code", "CreatedAt", "CustomerGroupId", "DebtAmount", "Email", "FullName", "IsActive", "Note", "Phone", "TaxCode", "TotalPurchased", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("d0000001-1111-2222-3333-444455556666"), "123 Đường Láng, Đống Đa, Hà Nội", "KH001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c24f74d1-55b2-4d2a-8742-5f657a8a25c1"), 0m, "vananh@gmail.com", "Nguyễn Văn Anh", true, "Khách hàng lẻ thân quen", "0912345678", null, 0m, null },
                    { new Guid("d0000002-1111-2222-3333-444455556666"), "456 Nguyễn Trãi, Thanh Xuân, Hà Nội", "KH002", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e5f02c6b-67a8-44a6-b51f-4b07fb7b4de1"), 0m, "thibinh@gmail.com", "Trần Thị Bình", true, "Khách hàng VIP mua nhiều", "0987654321", null, 1500000m, null },
                    { new Guid("d0000003-1111-2222-3333-444455556666"), "789 Lê Lợi, Quận 1, TP. Hồ Chí Minh", "KH003", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b8e18df0-fc7c-4869-9528-765f14841db1"), 2000000m, "hoangcuong@gmail.com", "Lê Hoàng Cường", true, "Đại lý bán sỉ tạp hóa nhỏ", "0905123456", "8012345678", 12000000m, null },
                    { new Guid("d0000004-1111-2222-3333-444455556666"), "101 Cầu Giấy, Quan Hoa, Hà Nội", "KH004", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c24f74d1-55b2-4d2a-8742-5f657a8a25c1"), 0m, "minhduc@gmail.com", "Phạm Minh Đức", true, "Khách vãng lai mua lẻ", "0936789012", null, 300000m, null },
                    { new Guid("d0000005-1111-2222-3333-444455556666"), "202 Trần Hưng Đạo, Hoàn Kiếm, Hà Nội", "KH005", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e5f02c6b-67a8-44a6-b51f-4b07fb7b4de1"), 0m, "thuha@gmail.com", "Đỗ Thu Hà", true, "Khách hàng VIP mua hàng tháng", "0977111222", null, 2500000m, null }
                });

            migrationBuilder.InsertData(
                table: "Promotions",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "Description", "DiscountType", "DiscountValue", "EndAt", "IsActive", "MinimumOrderAmount", "Name", "PromotionType", "StartAt", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a0000001-1111-2222-3333-444455556666"), "MUNGKHAITRUONG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), "Giảm giá 5% cho toàn bộ đơn hàng trị giá từ 100,000 VND trở lên", "Percent", 5m, new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 100000m, "Mừng Khai Trương Siêu Thị", "OrderDiscount", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000002-1111-2222-3333-444455556666"), "TRIANKHACHHANG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), "Giảm giá 10% cho toàn bộ đơn hàng trị giá từ 200,000 VND trở lên", "Percent", 10m, new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 200000m, "Tri Ân Khách Hàng Thân Thiết", "OrderDiscount", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "Id", "Address", "Code", "ContactEmail", "ContactPerson", "ContactPhone", "CreatedAt", "CreatedBy", "DebtAmount", "IsActive", "Name", "Note", "TaxCode", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("e0000001-1111-2222-3333-444455556666"), "Lô II-3, Đường số 11, KCN Tân Bình, Tân Phú, TP. HCM", "NCC-ACECOOK", "info@acecookvietnam.com", "Ông Kajiwara Junichi", "02838154060", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), 0m, true, "Công ty Cổ phần Acecook Việt Nam", "Nhà cung cấp các loại mì ăn liền Hảo Hảo", "0303525911", null },
                    { new Guid("e0000002-1111-2222-3333-444455556666"), "Lô A2-3, KCN Tây Bắc Củ Chi, Củ Chi, TP. HCM", "NCC-UNILEVER", "tuvankhachhang@unilever.com", "Bà Nguyễn Thị Bích Vân", "02854131100", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), 15000000m, true, "Công ty TNHH Unilever Việt Nam", "Nhà cung cấp Sunlight, OMO, Clear, Lifebuoy...", "0301438692", null },
                    { new Guid("e0000003-1111-2222-3333-444455556666"), "10 Tân Trào, Tân Phú, Quận 7, TP. HCM", "NCC-VINAMILK", "vinamilk@vinamilk.com.vn", "Bà Mai Kiều Liên", "02854155555", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), 0m, true, "Công ty Cổ phần Sữa Việt Nam (Vinamilk)", "Nhà cung cấp các sản phẩm sữa tươi, sữa chua, sữa đặc", "0300588569", null },
                    { new Guid("e0000004-1111-2222-3333-444455556666"), "Cao ốc Sheraton, 88 Đồng Khởi, Quận 1, TP. HCM", "NCC-PEPSICO", "contact@suntorypepsico.vn", "Ông Jahanzeb Khan", "02838219468", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), 8000000m, true, "Công ty TNHH Suntory PepsiCo Việt Nam", "Nhà cung cấp Pepsi, 7Up, Aquafina, Sting...", "0312051832", null },
                    { new Guid("e0000005-1111-2222-3333-444455556666"), "Tầng 18, Tòa nhà Vietcombank, 5 Công Trường Mê Linh, Quận 1, TP. HCM", "NCC-HEINEKEN", "heinekenvietnam@heineken.com", "Ông Alexander Koch", "02838222755", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), 0m, true, "Công ty TNHH Nhà Máy Bia Heineken Việt Nam", "Nhà cung cấp bia Heineken, Tiger, Larue...", "0300830053", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("d0000001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("d0000002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("d0000003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("d0000004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("d0000005-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Promotions",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Promotions",
                keyColumn: "Id",
                keyValue: new Guid("a0000002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("e0000001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("e0000002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("e0000003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("e0000004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("e0000005-1111-2222-3333-444455556666"));
        }
    }
}
