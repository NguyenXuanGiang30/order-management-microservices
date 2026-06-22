using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProductInventoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRealProductData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "ParentId", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Thực phẩm khô & Gia vị", null, 1, null },
                    { new Guid("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Nước giải khát, Bia & Nước suối", null, 2, null },
                    { new Guid("c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Sữa & Sản phẩm từ sữa", null, 3, null },
                    { new Guid("d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f9a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Bánh kẹo & Đồ ăn vặt", null, 4, null },
                    { new Guid("e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9a0b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Hóa mỹ phẩm & Chăm sóc cá nhân", null, 5, null }
                });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "Abbreviation", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("40c5f2b8-bd2e-4b20-8b1e-7f61b021c324"), "gói", true, "Gói" },
                    { new Guid("5a9602a8-12c8-472d-bf84-7a1a2b3c4d5e"), "lon", true, "Lon" },
                    { new Guid("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f"), "chai", true, "Chai" },
                    { new Guid("7c7804b0-34ea-494f-df06-9c3b4c5d6e7f"), "hộp", true, "Hộp" },
                    { new Guid("8d6905c1-45fb-4a50-ef17-0d4b5c6d7e8f"), "túi", true, "Túi" },
                    { new Guid("9e5a06d2-56fc-4b61-ff28-1e5b6c7d8e9f"), "hũ", true, "Hũ" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Barcode", "CategoryId", "Code", "CreatedAt", "Description", "ImageUrl", "ImportPrice", "IsActive", "Name", "SellPrice", "UnitId", "UpdatedAt", "Weight" },
                values: new object[,]
                {
                    { new Guid("f0010001-1111-2222-3333-444455556666"), "8934563138162", new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), "TP-MHH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mì ăn liền Hảo Hảo tôm chua cay 75g", null, 3200m, true, "Mì ăn liền Hảo Hảo tôm chua cay 75g", 4000m, new Guid("40c5f2b8-bd2e-4b20-8b1e-7f61b021c324"), null, 0.075m },
                    { new Guid("f0010002-1111-2222-3333-444455556666"), "8936011381014", new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), "TP-DAS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dầu ăn Simply nguyên chất 1L", null, 42000m, true, "Dầu ăn Simply nguyên chất 1L", 52000m, new Guid("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f"), null, 0.900m },
                    { new Guid("f0010003-1111-2222-3333-444455556666"), "8936017362352", new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), "TP-TCS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tương ớt Chinsu chai 250g", null, 10500m, true, "Tương ớt Chinsu chai 250g", 14000m, new Guid("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f"), null, 0.250m },
                    { new Guid("f0010004-1111-2222-3333-444455556666"), "8936017368026", new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), "TP-NMNN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nước mắm Nam Ngư 3 trong 1 chai 750ml", null, 32000m, true, "Nước mắm Nam Ngư 3 trong 1 chai 750ml", 39000m, new Guid("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f"), null, 0.750m },
                    { new Guid("f0010005-1111-2222-3333-444455556666"), "8934839111815", new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), "TP-HNKN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hạt nêm Knorr Thịt thăn Xương ống 400g", null, 28000m, true, "Hạt nêm Knorr Thịt thăn Xương ống 400g", 34000m, new Guid("40c5f2b8-bd2e-4b20-8b1e-7f61b021c324"), null, 0.400m },
                    { new Guid("f0010006-1111-2222-3333-444455556666"), "8938508216010", new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), "TP-GAO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gạo thơm Jasmine thượng hạng túi 5kg", null, 105000m, true, "Gạo thơm Jasmine thượng hạng túi 5kg", 125000m, new Guid("8d6905c1-45fb-4a50-ef17-0d4b5c6d7e8f"), null, 5.000m },
                    { new Guid("f0010007-1111-2222-3333-444455556666"), "8936017369016", new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), "TP-MKK", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mì gói Kokomi Đại 90 Tôm Chua Cay", null, 2800m, true, "Mì gói Kokomi Đại 90 Tôm Chua Cay", 3500m, new Guid("40c5f2b8-bd2e-4b20-8b1e-7f61b021c324"), null, 0.090m },
                    { new Guid("f0010008-1111-2222-3333-444455556666"), "8935041200055", new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"), "TP-MUOI", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Muối tinh sấy i-ốt Visalco 500g", null, 4000m, true, "Muối tinh sấy i-ốt Visalco 500g", 6000m, new Guid("40c5f2b8-bd2e-4b20-8b1e-7f61b021c324"), null, 0.500m },
                    { new Guid("f0020001-1111-2222-3333-444455556666"), "8935049500461", new Guid("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"), "NG-COCA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nước ngọt Coca Cola lon 320ml", null, 8000m, true, "Nước ngọt Coca Cola lon 320ml", 10000m, new Guid("5a9602a8-12c8-472d-bf84-7a1a2b3c4d5e"), null, 0.320m },
                    { new Guid("f0020002-1111-2222-3333-444455556666"), "8934822201332", new Guid("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"), "NG-KEN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bia Heineken Silver lon 330ml", null, 16000m, true, "Bia Heineken Silver lon 330ml", 20000m, new Guid("5a9602a8-12c8-472d-bf84-7a1a2b3c4d5e"), null, 0.330m },
                    { new Guid("f0020003-1111-2222-3333-444455556666"), "8935026810019", new Guid("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"), "NG-LAVIE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nước khoáng thiên nhiên La Vie 500ml", null, 4000m, true, "Nước khoáng thiên nhiên La Vie 500ml", 6000m, new Guid("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f"), null, 0.500m },
                    { new Guid("f0020004-1111-2222-3333-444455556666"), "8938502391010", new Guid("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"), "NG-PEPSI", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nước ngọt Pepsi lon 320ml", null, 7800m, true, "Nước ngọt Pepsi lon 320ml", 9500m, new Guid("5a9602a8-12c8-472d-bf84-7a1a2b3c4d5e"), null, 0.320m },
                    { new Guid("f0020005-1111-2222-3333-444455556666"), "8934822301339", new Guid("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"), "NG-TIGER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bia Tiger lon 330ml", null, 13500m, true, "Bia Tiger lon 330ml", 17000m, new Guid("5a9602a8-12c8-472d-bf84-7a1a2b3c4d5e"), null, 0.330m },
                    { new Guid("f0020006-1111-2222-3333-444455556666"), "8851012110115", new Guid("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"), "NG-REDBULL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nước tăng lực Redbull lon 250ml", null, 10000m, true, "Nước tăng lực Redbull lon 250ml", 12500m, new Guid("5a9602a8-12c8-472d-bf84-7a1a2b3c4d5e"), null, 0.250m },
                    { new Guid("f0020007-1111-2222-3333-444455556666"), "8936006030019", new Guid("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"), "NG-KHONGDO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Trà xanh Không Độ 500ml", null, 7000m, true, "Trà xanh Không Độ 500ml", 9000m, new Guid("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f"), null, 0.500m },
                    { new Guid("f0020008-1111-2222-3333-444455556666"), "8938502391034", new Guid("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"), "NG-7UP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nước ngọt 7Up lon 320ml", null, 7800m, true, "Nước ngọt 7Up lon 320ml", 9500m, new Guid("5a9602a8-12c8-472d-bf84-7a1a2b3c4d5e"), null, 0.320m },
                    { new Guid("f0030001-1111-2222-3333-444455556666"), "8934673122457", new Guid("c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f"), "SU-VNM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sữa tươi tiệt trùng Vinamilk ít đường 180ml", null, 6500m, true, "Sữa tươi tiệt trùng Vinamilk ít đường 180ml", 8500m, new Guid("7c7804b0-34ea-494f-df06-9c3b4c5d6e7f"), null, 0.180m },
                    { new Guid("f0030002-1111-2222-3333-444455556666"), "8936049080036", new Guid("c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f"), "SU-TH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sữa tươi tiệt trùng TH True Milk ít đường 180ml", null, 6800m, true, "Sữa tươi tiệt trùng TH True Milk ít đường 180ml", 9000m, new Guid("7c7804b0-34ea-494f-df06-9c3b4c5d6e7f"), null, 0.180m },
                    { new Guid("f0030003-1111-2222-3333-444455556666"), "8934804021200", new Guid("c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f"), "SU-MILO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sữa lúa mạch Nestlé Milo hộp 180ml", null, 7000m, true, "Sữa lúa mạch Nestlé Milo hộp 180ml", 9200m, new Guid("7c7804b0-34ea-494f-df06-9c3b4c5d6e7f"), null, 0.180m },
                    { new Guid("f0030004-1111-2222-3333-444455556666"), "8934673130179", new Guid("c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f"), "SU-CHUA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sữa chua ăn Vinamilk có đường hộp 100g", null, 5000m, true, "Sữa chua ăn Vinamilk có đường hộp 100g", 6500m, new Guid("7c7804b0-34ea-494f-df06-9c3b4c5d6e7f"), null, 0.100m },
                    { new Guid("f0030005-1111-2222-3333-444455556666"), "8934673120156", new Guid("c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f"), "SU-ONTHO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sữa đặc Ông Thọ đỏ lon 380g", null, 22000m, true, "Sữa đặc Ông Thọ đỏ lon 380g", 26000m, new Guid("5a9602a8-12c8-472d-bf84-7a1a2b3c4d5e"), null, 0.380m },
                    { new Guid("f0040001-1111-2222-3333-444455556666"), "8934681144007", new Guid("d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f9a"), "BK-COSY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bánh quy Cosy Kinh Đô Marie gói 144g", null, 12000m, true, "Bánh quy Cosy Kinh Đô Marie gói 144g", 16000m, new Guid("40c5f2b8-bd2e-4b20-8b1e-7f61b021c324"), null, 0.144m },
                    { new Guid("f0040002-1111-2222-3333-444455556666"), "8936079010123", new Guid("d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f9a"), "BK-LAYS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khoai tây chiên Lay's vị tự nhiên gói 95g", null, 16000m, true, "Khoai tây chiên Lay's vị tự nhiên gói 95g", 21000m, new Guid("40c5f2b8-bd2e-4b20-8b1e-7f61b021c324"), null, 0.095m },
                    { new Guid("f0040003-1111-2222-3333-444455556666"), "8936036010041", new Guid("d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f9a"), "BK-CHOCO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bánh ChocoPie Orion hộp 12 cái 396g", null, 44000m, true, "Bánh ChocoPie Orion hộp 12 cái 396g", 55000m, new Guid("7c7804b0-34ea-494f-df06-9c3b4c5d6e7f"), null, 0.396m },
                    { new Guid("f0040004-1111-2222-3333-444455556666"), "4001686301524", new Guid("d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f9a"), "BK-HARIBO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kẹo dẻo Haribo Goldbears gói 80g", null, 22000m, true, "Kẹo dẻo Haribo Goldbears gói 80g", 28000m, new Guid("40c5f2b8-bd2e-4b20-8b1e-7f61b021c324"), null, 0.080m },
                    { new Guid("f0040005-1111-2222-3333-444455556666"), "8886467100017", new Guid("d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f9a"), "BK-PRINGLES", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Snack khoai tây Pringles vị tự nhiên 110g", null, 32000m, true, "Snack khoai tây Pringles vị tự nhiên 110g", 40000m, new Guid("7c7804b0-34ea-494f-df06-9c3b4c5d6e7f"), null, 0.110m },
                    { new Guid("f0040006-1111-2222-3333-444455556666"), "022000004944", new Guid("d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f9a"), "BK-GUM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kẹo cao su Doublemint sọc 5 tép", null, 4000m, true, "Kẹo cao su Doublemint sọc 5 tép", 6000m, new Guid("40c5f2b8-bd2e-4b20-8b1e-7f61b021c324"), null, 0.015m },
                    { new Guid("f0050001-1111-2222-3333-444455556666"), "8934839130779", new Guid("e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9a0b"), "HM-SL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nước rửa chén Sunlight chanh chai 750g", null, 24000m, true, "Nước rửa chén Sunlight chanh chai 750g", 30000m, new Guid("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f"), null, 0.750m },
                    { new Guid("f0050002-1111-2222-3333-444455556666"), "8934839141089", new Guid("e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9a0b"), "HM-OMO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bột giặt OMO Comfort tinh dầu thơm túi 3.6kg", null, 165000m, true, "Bột giặt OMO Comfort tinh dầu thơm túi 3.6kg", 198000m, new Guid("8d6905c1-45fb-4a50-ef17-0d4b5c6d7e8f"), null, 3.600m },
                    { new Guid("f0050003-1111-2222-3333-444455556666"), "6901396340243", new Guid("e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9a0b"), "HM-CG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kem đánh răng Colgate ngừa sâu răng 225g", null, 28000m, true, "Kem đánh răng Colgate ngừa sâu răng 225g", 35000m, new Guid("7c7804b0-34ea-494f-df06-9c3b4c5d6e7f"), null, 0.225m },
                    { new Guid("f0050004-1111-2222-3333-444455556666"), "8934839154386", new Guid("e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9a0b"), "HM-CLEAR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dầu gội Clear Bạc Hà thơm mát chai 630ml", null, 125000m, true, "Dầu gội Clear Bạc Hà thơm mát chai 630ml", 149000m, new Guid("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f"), null, 0.630m },
                    { new Guid("f0050005-1111-2222-3333-444455556666"), "8934839129056", new Guid("e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9a0b"), "HM-LIFEBUOY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sữa tắm Lifebuoy bảo vệ vượt trội chai 850g", null, 140000m, true, "Sữa tắm Lifebuoy bảo vệ vượt trội chai 850g", 169000m, new Guid("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f"), null, 0.850m },
                    { new Guid("f0050006-1111-2222-3333-444455556666"), "8934839123016", new Guid("e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9a0b"), "HM-LAUSAN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nước lau sàn Sunlight Hương Hoa thiên nhiên 1kg", null, 23000m, true, "Nước lau sàn Sunlight Hương Hoa thiên nhiên 1kg", 29000m, new Guid("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f"), null, 1.000m },
                    { new Guid("f0050007-1111-2222-3333-444455556666"), "4902430882101", new Guid("e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9a0b"), "HM-DOWNY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nước xả vải Downy Huyền Bí túi 1.6L", null, 110000m, true, "Nước xả vải Downy Huyền Bí túi 1.6L", 135000m, new Guid("8d6905c1-45fb-4a50-ef17-0d4b5c6d7e8f"), null, 1.600m }
                });

            migrationBuilder.InsertData(
                table: "Inventories",
                columns: new[] { "Id", "LastUpdated", "MaxThreshold", "MinThreshold", "ProductId", "QuantityOnHand", "QuantityReserved" },
                values: new object[,]
                {
                    { new Guid("e0010001-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0010001-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0010002-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0010002-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0010003-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0010003-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0010004-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0010004-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0010005-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0010005-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0010006-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0010006-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0010007-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0010007-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0010008-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0010008-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0020001-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0020001-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0020002-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0020002-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0020003-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0020003-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0020004-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0020004-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0020005-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0020005-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0020006-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0020006-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0020007-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0020007-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0020008-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0020008-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0030001-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0030001-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0030002-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0030002-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0030003-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0030003-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0030004-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0030004-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0030005-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0030005-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0040001-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0040001-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0040002-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0040002-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0040003-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0040003-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0040004-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0040004-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0040005-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0040005-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0040006-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0040006-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0050001-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0050001-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0050002-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0050002-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0050003-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0050003-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0050004-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0050004-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0050005-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0050005-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0050006-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0050006-1111-2222-3333-444455556666"), 100, 0 },
                    { new Guid("e0050007-1111-2222-3333-444455556666"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000, 10, new Guid("f0050007-1111-2222-3333-444455556666"), 100, 0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0010001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0010002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0010003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0010004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0010005-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0010006-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0010007-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0010008-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0020001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0020002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0020003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0020004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0020005-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0020006-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0020007-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0020008-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0030001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0030002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0030003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0030004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0030005-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0040001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0040002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0040003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0040004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0040005-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0040006-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0050001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0050002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0050003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0050004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0050005-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0050006-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: new Guid("e0050007-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "Id",
                keyValue: new Guid("9e5a06d2-56fc-4b61-ff28-1e5b6c7d8e9f"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0010001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0010002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0010003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0010004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0010005-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0010006-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0010007-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0010008-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0020001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0020002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0020003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0020004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0020005-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0020006-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0020007-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0020008-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0030001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0030002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0030003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0030004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0030005-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0040001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0040002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0040003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0040004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0040005-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0040006-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0050001-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0050002-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0050003-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0050004-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0050005-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0050006-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0050007-1111-2222-3333-444455556666"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f9a"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9a0b"));

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "Id",
                keyValue: new Guid("40c5f2b8-bd2e-4b20-8b1e-7f61b021c324"));

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "Id",
                keyValue: new Guid("5a9602a8-12c8-472d-bf84-7a1a2b3c4d5e"));

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "Id",
                keyValue: new Guid("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f"));

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "Id",
                keyValue: new Guid("7c7804b0-34ea-494f-df06-9c3b4c5d6e7f"));

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "Id",
                keyValue: new Guid("8d6905c1-45fb-4a50-ef17-0d4b5c6d7e8f"));
        }
    }
}
