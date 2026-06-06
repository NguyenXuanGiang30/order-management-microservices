# Đặc Tả Chức Năng Cho Thiết Kế Lại UI

Tài liệu này mô tả lại toàn bộ chức năng hiện có của dự án RetailOps để dùng làm nền khi thiết kế và code lại giao diện. Nội dung tập trung vào nghiệp vụ, vai trò người dùng, luồng thao tác, dữ liệu cần hiển thị, trạng thái UI và các API đang tồn tại. Đây không phải mô tả visual hiện tại.

## 1. Tổng Quan Sản Phẩm

RetailOps là hệ thống quản lý bán lẻ, kho hàng và báo cáo cho cửa hàng tạp hóa, siêu thị mini hoặc điểm bán nhỏ. Hệ thống gồm frontend Vue 3/Vuetify và backend dạng nhiều service.

Các phân hệ chính:

1. Xác thực, phân quyền, phiên đăng nhập.
2. Bán hàng POS tại quầy.
3. Quản lý đơn hàng, hóa đơn, trả hàng, hủy đơn.
4. Quản lý khách hàng và công nợ khách hàng.
5. Quản lý sản phẩm, barcode, giá bán, giá nhập.
6. Quản lý tồn kho, cảnh báo thấp kho, lịch sử biến động kho.
7. Nhập hàng từ nhà cung cấp.
8. Kiểm kê kho.
9. Quản lý nhà cung cấp và công nợ nhà cung cấp.
10. Khuyến mãi.
11. Ca bán hàng.
12. Thanh toán, ghi nhận tiền, đối soát chuyển khoản/VietQR.
13. Báo cáo doanh thu, lợi nhuận, top sản phẩm, top khách hàng.
14. Quản trị người dùng, quyền, audit log, thông báo, backup/restore.

## 2. Vai Trò Người Dùng

### 2.1. Admin

Mục tiêu: quản trị toàn hệ thống, xem báo cáo, kiểm soát dữ liệu, có thể thao tác cả bán hàng và kho.

Quyền mặc định:

- `dashboard.read`
- `orders.read`
- `orders.write`
- `orders.return`
- `pos.sell`
- `inventory.read`
- `inventory.write`
- `stocktake.write`
- `purchasing.write`
- `customers.read`
- `customers.write`
- `suppliers.read`
- `suppliers.write`
- `reports.read`
- `reports.profit`
- `audit.read`
- `users.manage`
- `permissions.manage`
- `notifications.read`
- `backup.create`
- `backup.restore`

Điều hướng UI:

- Có sidebar.
- Có topbar với thông báo, đổi theme, menu tài khoản.
- Menu chính: Tổng quan, POS bán lẻ, Đơn hàng, Kho hàng, Nhập kho, Công nợ, Quản trị.

### 2.2. Sales

Mục tiêu: bán hàng tại quầy, theo dõi đơn hàng, xử lý thanh toán và công nợ khách hàng.

Quyền mặc định:

- `dashboard.read`
- `pos.sell`
- `orders.read`
- `orders.write`
- `orders.return`
- `customers.read`
- `customers.write`
- `notifications.read`

Điều hướng UI:

- Không có sidebar.
- Dùng workspace tabs phía trên.
- Tabs: Bán hàng, Đơn hàng, Công nợ.
- Không được vào Kho hàng hoặc Nhập hàng.

### 2.3. Warehouse

Mục tiêu: quản lý tồn kho, nhập hàng, kiểm kê, nhà cung cấp và công nợ nhà cung cấp.

Quyền mặc định:

- `dashboard.read`
- `inventory.read`
- `inventory.write`
- `stocktake.write`
- `purchasing.write`
- `suppliers.read`
- `suppliers.write`
- `notifications.read`

Điều hướng UI:

- Không có sidebar.
- Dùng workspace tabs phía trên.
- Tabs: Tồn kho, Nhập hàng, Nhà cung cấp.
- Không được vào POS hoặc Đơn hàng.

## 3. Shell Ứng Dụng

### 3.1. Khi Chưa Đăng Nhập

Chỉ hiển thị màn đăng nhập. Không hiển thị app shell, sidebar, tabs hoặc nội dung nghiệp vụ.

### 3.2. Khi Đã Đăng Nhập

App shell cần có:

- Topbar cố định, cao vừa phải.
- Brand/Logo hệ thống.
- Tên người dùng và vai trò.
- Menu đăng xuất.
- Nút đổi light/dark theme.
- Nút thông báo với badge số chưa đọc.
- Vùng lỗi backend nếu API không tải được.
- Loading bar khi đang tải dữ liệu nền.
- Guard màn hình nếu user không có quyền.

### 3.3. Dữ Liệu Load Khi Vào App

Khi user đăng nhập, frontend hiện đang load:

- Products từ `/api/products`.
- Stock từ `/api/inventory/stock`.
- Suppliers nếu role là Admin hoặc Warehouse.
- Customers và Orders nếu role là Admin hoặc Sales.
- Notification preview và unread count nếu user có `notifications.read`.

Yêu cầu UI mới:

- Phải tách rõ trạng thái loading, empty, error.
- Không để màn hình trắng khi backend lỗi.
- Có nút thử lại.
- Không cho user bấm vào tab không thuộc role.

## 4. Màn Đăng Nhập

### Mục tiêu

Xác thực user và đưa user vào workspace đúng vai trò.

### Dữ liệu form

- Username.
- Password.
- Show/hide password.

### Hành động

- Submit login.
- Hiển thị loading.
- Hiển thị lỗi đăng nhập hoặc lỗi backend.
- Lưu `currentUser` vào localStorage sau khi đăng nhập.

### API

- `POST /api/auth/login`

Response chuẩn hóa gồm:

- accessToken.
- refreshToken.
- expiresIn.
- user id.
- username.
- fullName.
- role: `Admin`, `Sales`, `Warehouse`.
- permissions.

### Trạng thái UI cần thiết kế

- Default.
- Focus input.
- Password visible.
- Loading submit.
- Error 401 sai tài khoản/mật khẩu.
- Error 502/backend down.
- Mobile layout.

Ghi chú thiết kế: login hiện tại cần làm lại hoàn toàn. Không nên dùng hero chữ quá lớn khiến layout dính lên trên. Nên dùng auth card centered, max width rõ ràng, có brand panel vừa phải hoặc bỏ panel ở màn nhỏ.

## 5. Dashboard

### Mục tiêu

Cho Admin hoặc người có quyền xem tổng quan nhanh về vận hành.

### Dữ liệu hiện dùng

- Doanh thu từ `state.posSales`.
- Tổng số hàng tồn từ `state.products`.
- Sản phẩm thấp kho từ `quantityOnHand <= minThreshold`.
- Công nợ khách hàng từ `state.customers`.
- Công nợ nhà cung cấp từ `state.suppliers`.

### Thành phần UI

- Page header: tiêu đề, mô tả, trạng thái dữ liệu.
- KPI cards:
  - Doanh thu hôm nay.
  - Tổng tồn kho.
  - Số SKU cần nhập.
  - Công nợ.
- Chart doanh số theo giờ.
- Panel cảnh báo tồn kho.
- Empty state nếu chưa có giao dịch.

### Hành động

- Làm mới dữ liệu.
- Từ cảnh báo kho bấm sang Nhập hàng.

### API có thể dùng thêm

- `GET /api/reports/dashboard`
- `GET /api/reports/daily`
- `GET /api/reports/monthly`
- `GET /api/reports/top-products`
- `GET /api/reports/top-customers`

### Thiết kế lại cần chú ý

- Dashboard phải là màn operational, không phải landing page.
- KPI cần gọn, dễ scan, không quá nhiều màu.
- Chart không được chiếm quá nhiều chiều cao.
- Số liệu tiền phải canh phải hoặc nổi bật nhất quán.

## 6. POS Bán Hàng

### Mục tiêu

Màn thao tác chính của nhân viên bán hàng: tìm sản phẩm, quét barcode, thêm vào giỏ, áp khuyến mãi, chọn khách hàng, thanh toán và in hóa đơn.

### Dữ liệu chính

Product:

- id.
- name.
- sku/code.
- barcode.
- category.
- sell price.
- import price nếu cần tính cost.
- quantityOnHand.
- availableQuantity.
- minThreshold.
- unit.

Cart item:

- product.
- quantity.
- line total.

Customer:

- id.
- name.
- phone.
- group.
- discount.
- totalSpent.
- debt.

Cash shift:

- shiftCode.
- cashier.
- openedAt.
- openingCash.
- expectedCash.
- actualCash.
- variance.
- status.

Promotion:

- code.
- name.
- promotionType: Order/Product/Combo.
- discountType: Percent/FixedAmount.
- discountValue.
- minimumOrderAmount.
- startAt/endAt.
- items.

### Thành phần UI

- Header POS: trạng thái ca, tổng số item trong giỏ.
- Search/autocomplete sản phẩm theo tên, SKU, barcode.
- Nút quét barcode/QR bằng camera.
- Bộ lọc category.
- Lưới sản phẩm.
- Badge cảnh báo thấp kho.
- Cart panel.
- Tăng/giảm số lượng.
- Xóa dòng hàng.
- Chọn khách hàng.
- Chọn khuyến mãi đang chạy.
- Nhập mã khuyến mãi thủ công.
- Tóm tắt tiền:
  - Tạm tính.
  - Giảm theo nhóm khách hàng.
  - Giảm theo khuyến mãi.
  - Thành tiền.
- Chọn phương thức thanh toán:
  - Tiền mặt.
  - Chuyển khoản.
  - Ghi nợ.
- Cấu hình cửa hàng/VietQR.
- Dialog hóa đơn.
- Dialog VietQR.

### Luồng thao tác chuẩn

1. Mở ca bán hàng nếu chưa có ca.
2. Tìm hoặc quét barcode sản phẩm.
3. Thêm sản phẩm vào giỏ.
4. Kiểm tra tồn kho, không cho bán vượt tồn hợp lệ.
5. Chọn khách hàng.
6. Chọn hoặc nhập khuyến mãi.
7. Chọn phương thức thanh toán.
8. Tạo đơn hàng.
9. Nếu tiền mặt: ghi nhận đã thanh toán ngay.
10. Nếu chuyển khoản: hiển thị VietQR, chờ đối soát giao dịch.
11. Nếu ghi nợ: tạo đơn còn debtAmount.
12. Hiển thị/in hóa đơn.
13. Làm rỗng giỏ.

### API

- `GET /api/shifts/current`
- `POST /api/shifts/open`
- `POST /api/shifts/{id}/close`
- `GET /api/products`
- `GET /api/inventory/stock`
- `GET /api/customers`
- `POST /api/customers`
- `GET /api/promotions/active`
- `GET /api/promotions/{code}/validate`
- `POST /api/orders`
- `POST /api/payments`
- `GET /vqr/transactions?orderCode=...`

### Trạng thái cần thiết kế

- Chưa mở ca.
- Đang mở ca.
- Đóng ca.
- Giỏ trống.
- Sản phẩm hết hàng.
- Sản phẩm thấp kho.
- Khuyến mãi không hợp lệ.
- Đang checkout.
- Checkout thành công.
- Chờ chuyển khoản.
- Chuyển khoản đã khớp.
- Chuyển khoản thiếu/thừa tiền.
- Ghi nợ thiếu khách hàng.

### Thiết kế lại cần chú ý

- POS cần ưu tiên tốc độ thao tác, không trang trí nặng.
- Trên desktop nên có layout 2 vùng: catalog trái, cart phải.
- Trên mobile nên cân nhắc bottom cart hoặc tab Sản phẩm/Giỏ hàng.
- Nút thanh toán phải nổi bật nhưng không dùng gradient quá gắt.
- Search và barcode phải là entry point chính.

## 7. Quản Lý Đơn Hàng

### Mục tiêu

Theo dõi toàn bộ đơn đã tạo, xem chi tiết, in hóa đơn, xác nhận thanh toán, trả hàng hoặc hủy đơn.

### Dữ liệu đơn hàng

- id.
- orderCode.
- customerId.
- customerName.
- createdByName.
- orderDate.
- subTotal.
- discountPercent.
- discountAmount.
- promotionCode.
- promotionName.
- promotionDiscountAmount.
- finalAmount.
- paidAmount.
- debtAmount.
- paymentMethod.
- status.
- note.
- orderDetails.

### Thành phần UI

- Search theo mã đơn hoặc tên khách.
- Filter phương thức thanh toán.
- Filter trạng thái.
- KPI nhỏ:
  - Tổng doanh thu.
  - Tổng đơn.
  - Đơn còn nợ.
  - Đơn đã hủy/trả.
- Bảng đơn hàng:
  - Mã đơn.
  - Khách hàng.
  - Thời gian.
  - Sản phẩm tóm tắt.
  - Tổng tiền.
  - Phương thức thanh toán.
  - Trạng thái.
  - Hành động.
- Dialog chi tiết hóa đơn.
- Dialog trả hàng.
- Pagination.

### Hành động

- Tải lại đơn.
- Xem chi tiết.
- In hóa đơn.
- Xác nhận thanh toán thủ công.
- Hủy đơn.
- Tạo phiếu trả hàng một phần hoặc toàn phần.

### API

- `GET /api/orders`
- `GET /api/orders/{id}`
- `POST /api/orders`
- `PUT /api/orders/{id}`
- `PUT /api/orders/{id}/cancel`
- `POST /api/orders/{id}/return`
- `GET /api/orders/{id}/invoice`
- `GET /api/orders/{id}/status-history`
- `POST /api/payments`

### Trạng thái cần thiết kế

- Loading table.
- Empty result.
- Filter empty.
- Paid.
- Pending.
- Debt.
- Partially paid.
- Returned.
- Cancelled.
- Action loading theo từng dòng.
- Confirm dialog cho hủy đơn/trả hàng.

### Lưu ý quyền

- Admin có quyền hủy đơn.
- Sales có quyền đọc/ghi đơn và trả hàng theo permissions hiện tại, nhưng backend cancel order đang khóa Admin.

## 8. Quản Lý Tồn Kho

### Mục tiêu

Xem tồn kho vật lý, tồn khả dụng, cảnh báo thấp kho, gợi ý nhập hàng, lịch sử kiểm kê và export dữ liệu.

### Dữ liệu stock

- productId.
- productCode.
- productName.
- unitName.
- quantityOnHand.
- quantityReserved.
- availableQuantity.
- minThreshold.
- maxThreshold.
- alertLevel.
- reorderQuantity.
- recommendedOrderQuantity.
- stockCoverageLabel.

### Thành phần UI

- Search theo tên sản phẩm, SKU, barcode.
- Filter cảnh báo:
  - Tất cả.
  - Cần nhập.
  - Hết hàng.
  - An toàn.
- Metric cards:
  - Tổng SKU.
  - Cần nhập.
  - Hết hàng.
  - Đang kiểm kê.
- Bảng tồn kho:
  - SKU/barcode.
  - Sản phẩm.
  - Danh mục.
  - Giá bán.
  - Tồn.
  - Khả dụng.
  - Ngưỡng min/max.
  - Cảnh báo.
  - Gợi ý nhập.
- Lịch sử kiểm kê.
- Dialog kiểm kê.
- Export tồn kho.
- Export giao dịch kho.

### Hành động

- Tải lại tồn kho.
- Export tồn kho CSV.
- Export giao dịch kho CSV.
- Tạo phiên kiểm kê.
- Mở phiên kiểm kê.
- Nhập số lượng thực tế.
- Import CSV kiểm kê.
- Export mẫu kiểm kê.
- Xác nhận kiểm kê.

### API

- `GET /api/inventory/stock`
- `GET /api/inventory/transactions`
- `GET /api/inventory/stock/export`
- `GET /api/inventory/transactions/export`
- `GET /api/inventory/stocktakes`
- `GET /api/inventory/stocktakes/{id}`
- `POST /api/inventory/stocktakes`
- `PUT /api/inventory/stocktakes/{id}/lines`
- `PUT /api/inventory/stocktakes/{id}/confirm`
- `PUT /api/inventory/stocktakes/{id}/cancel`
- `GET /api/inventory/stocktakes/{id}/template`
- `POST /api/inventory/stocktakes/{id}/import`

### Trạng thái cần thiết kế

- Loading stock.
- Empty stock.
- Low stock.
- Out of stock.
- Stocktake draft.
- Stocktake confirmed.
- Import file error.
- Count variance positive/negative/zero.

## 9. Nhập Kho / Goods Receipt

### Mục tiêu

Tạo phiếu nhập hàng từ nhà cung cấp, nhập nhiều dòng sản phẩm, quét SKU/barcode để thêm nhanh, xác nhận phiếu để cộng tồn kho và cập nhật giá nhập trung bình.

### Dữ liệu phiếu nhập

- id.
- receiptCode/code.
- supplierId.
- supplierName.
- createdBy.
- createdByName.
- receiptDate/createdAt.
- status: Draft, Confirmed, Cancelled.
- note.
- details/items:
  - productId.
  - productName.
  - quantity.
  - unitPrice/importPrice.
  - subTotal.
- totalAmount.

### Thành phần UI

- Header có nút tạo phiếu nhập.
- Bảng phiếu nhập:
  - Mã phiếu.
  - Nhà cung cấp.
  - Ngày tạo.
  - Số mặt hàng.
  - Tổng tiền.
  - Trạng thái.
  - Hành động.
- Dialog chi tiết phiếu.
- Dialog tạo phiếu mới:
  - Chọn nhà cung cấp.
  - Chọn sản phẩm.
  - Nhập/quét SKU hoặc barcode.
  - Số lượng.
  - Giá nhập.
  - Ghi chú.
  - Bảng dòng nháp.
  - Tổng cộng.

### Hành động

- Tải lại danh sách.
- Tạo phiếu nháp.
- Thêm dòng bằng chọn sản phẩm.
- Thêm dòng nhanh bằng SKU/barcode.
- Nếu sản phẩm đã có trong phiếu, cộng dồn số lượng.
- Xóa dòng nháp.
- Xem chi tiết phiếu.
- Xác nhận phiếu.
- Hủy phiếu nháp.

### API

- `GET /api/inventory/receipts`
- `POST /api/inventory/receipts`
- `PUT /api/inventory/receipts/{id}/confirm`
- `PUT /api/inventory/receipts/{id}/cancel`
- `GET /api/suppliers`
- `GET /api/products`

### Luồng xác nhận phiếu

Khi xác nhận:

1. Phiếu phải ở trạng thái Draft.
2. Hệ thống cộng số lượng vào tồn kho.
3. Hệ thống ghi lịch sử giao dịch kho.
4. Hệ thống cập nhật `Product.ImportPrice` theo weighted average.
5. Hệ thống phát event `GoodsReceiptConfirmedEvent`.

### Thiết kế lại cần chú ý

- Đây là màn quan trọng của thủ kho, cần thao tác nhanh với nhiều dòng.
- Nên có table editable hoặc drawer chi tiết thay vì dialog quá chật.
- Cần rõ khác biệt giữa giá nhập và giá bán.
- Cần hỗ trợ nhập nhiều loại hàng trong một phiếu.

## 10. Kiểm Kê Kho

### Mục tiêu

Tạo phiên kiểm kê để so sánh số lượng hệ thống và số lượng thực tế, sau đó xác nhận để điều chỉnh tồn.

### Dữ liệu

Stocktake session:

- id.
- stocktakeCode.
- countedByName.
- status.
- startedAt.
- confirmedAt.
- note.
- totalItems.
- countedItems.
- totalVarianceQuantity.
- lines.

Stocktake line:

- id.
- productId.
- productCode.
- productName.
- unitName.
- systemQuantity.
- countedQuantity.
- varianceQuantity.
- note.

### Thành phần UI

- Danh sách phiên kiểm kê.
- Trạng thái Draft/Confirmed/Cancelled.
- Progress đã đếm/tổng số.
- Dialog hoặc page chi tiết kiểm kê.
- Bảng dòng kiểm kê.
- Input số lượng thực tế.
- Cột chênh lệch.
- Actions export/import/confirm.

### Hành động

- Tạo phiên kiểm kê.
- Mở phiên.
- Cập nhật dòng đếm.
- Import CSV.
- Export mẫu CSV.
- Xác nhận kiểm kê.
- Hủy phiên kiểm kê.

### Trạng thái UI

- Draft cho phép nhập.
- Confirmed chỉ xem.
- Dòng chưa đếm.
- Dòng lệch dương.
- Dòng lệch âm.
- Dòng không lệch.

## 11. Công Nợ

### Mục tiêu

Theo dõi công nợ khách hàng và công nợ nhà cung cấp.

### Dữ liệu khách hàng

- id.
- name.
- phone.
- group.
- discount.
- totalSpent.
- debt.

### Dữ liệu nhà cung cấp

- id.
- code.
- name.
- phone.
- email.
- address.
- taxCode.
- debt.

### Thành phần UI

- Tổng phải thu khách hàng.
- Tổng phải trả nhà cung cấp.
- Danh sách khách hàng còn nợ.
- Danh sách nhà cung cấp còn nợ.
- Thông tin liên hệ.
- Nút thu hồi nợ.
- Nút thanh toán nợ.
- Dialog nhập số tiền thanh toán.

### Hành động

- Thu nợ khách hàng.
- Thanh toán nợ nhà cung cấp.
- Cập nhật số dư hiển thị.

### API liên quan

- `GET /api/customers`
- `GET /api/customers/{id}/debt`
- `POST /api/payments`
- `GET /api/suppliers`

Ghi chú: UI hiện tại đang xử lý một phần công nợ bằng state local. Khi thiết kế lại nên chuẩn hóa luồng thanh toán công nợ qua API rõ ràng.

## 12. Khách Hàng

### Mục tiêu

Quản lý khách hàng để bán hàng, chiết khấu theo nhóm, ghi nợ và theo dõi lịch sử mua.

### Dữ liệu

- code.
- fullName.
- phone.
- email.
- address.
- taxCode.
- customerGroupId.
- customerGroupName.
- totalPurchased.
- debtAmount.

### API

- `GET /api/customers`
- `GET /api/customers/{id}`
- `POST /api/customers`
- `PUT /api/customers/{id}`
- `GET /api/customers/{id}/debt`

### UI cần có trong redesign

Hiện chưa có màn khách hàng riêng đầy đủ. Nên thêm:

- Danh sách khách hàng.
- Search.
- Tạo/sửa khách hàng.
- Xem lịch sử mua.
- Xem công nợ.
- Gắn nhóm khách hàng.

## 13. Nhóm Khách Hàng

### Mục tiêu

Quản lý nhóm khách và chiết khấu mặc định.

### API có backend

- `GET /api/customer-groups`
- `POST /api/customer-groups`
- `PUT /api/customer-groups/{id}`
- `DELETE /api/customer-groups/{id}`

### UI cần có

Hiện chưa có màn riêng. Nên nằm trong Admin hoặc Customers:

- Danh sách nhóm.
- Tên nhóm.
- % giảm mặc định.
- Ghi chú.
- Tạo/sửa/xóa.

## 14. Sản Phẩm, Barcode, Danh Mục, Đơn Vị

### Mục tiêu

Quản lý catalog sản phẩm phục vụ bán hàng và kho.

### Dữ liệu sản phẩm

- id.
- code/SKU.
- name.
- description.
- barcode.
- categoryId/categoryName.
- unitId/unitName.
- sellPrice.
- importPrice.
- minThreshold.
- maxThreshold.
- isActive.

### API sản phẩm

- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products`
- `PUT /api/products/{id}`
- `PUT /api/products/{id}/toggle-active`

### API danh mục

- `GET /api/categories`
- `POST /api/categories`
- `PUT /api/categories/{id}`
- `DELETE /api/categories/{id}`

### API đơn vị

- `GET /api/units`
- `POST /api/units`

### UI cần có trong redesign

Hiện UI catalog CRUD chưa đầy đủ. Nên có màn Product Management:

- Danh sách sản phẩm.
- Search theo tên/SKU/barcode.
- Filter category/status/low stock.
- Tạo sản phẩm.
- Sửa sản phẩm.
- Bật/tắt bán.
- Quản lý barcode.
- Quản lý giá bán và giá nhập.
- Cảnh báo thiếu barcode.
- Import/export nếu cần.

## 15. Nhà Cung Cấp

### Mục tiêu

Quản lý nhà cung cấp phục vụ nhập hàng và công nợ.

### Dữ liệu

- code.
- name.
- contactPerson.
- contactPhone.
- contactEmail.
- address.
- taxCode.
- note.
- debtAmount.

### API

- `GET /api/suppliers`
- `POST /api/suppliers`
- `PUT /api/suppliers/{id}`

### UI cần có

Hiện Warehouse tab đang gọi là Nhà cung cấp nhưng dùng DebtView. Nên tách thành:

- Supplier list.
- Supplier debt.
- Supplier detail.
- Create/edit supplier.
- Danh sách phiếu nhập theo supplier.

## 16. Khuyến Mãi

### Mục tiêu

Áp dụng khuyến mãi vào đơn hàng POS.

### Dữ liệu

- code.
- name.
- description.
- promotionType.
- discountType.
- discountValue.
- minimumOrderAmount.
- startAt.
- endAt.
- isActive.
- items cho combo/product promotion.

### API

- `GET /api/promotions/active`
- `GET /api/promotions/{code}/validate`
- `POST /api/promotions` Admin.
- `PUT /api/promotions/{id}` Admin.
- `PUT /api/promotions/{id}/toggle-active` Admin.

### UI hiện có

- POS có chọn khuyến mãi đang chạy.
- POS có nhập mã khuyến mãi thủ công.

### UI cần có thêm

- Admin quản lý promotion:
  - Danh sách khuyến mãi.
  - Tạo/sửa.
  - Bật/tắt.
  - Cấu hình loại giảm giá.
  - Cấu hình điều kiện đơn tối thiểu.
  - Cấu hình sản phẩm combo.

## 17. Ca Bán Hàng

### Mục tiêu

Kiểm soát tiền đầu ca, tiền cuối ca và sai lệch khi đóng ca.

### Dữ liệu

- shiftCode.
- cashierId.
- cashierName.
- openedAt.
- openingCash.
- closedAt.
- expectedCash.
- actualCash.
- variance.
- status.
- note.

### API

- `GET /api/shifts/current`
- `GET /api/shifts`
- `POST /api/shifts/open`
- `POST /api/shifts/{id}/close`

### UI hiện có

- POS có panel mở/đóng ca.

### UI cần có thêm

- Admin/Sales có lịch sử ca.
- Chi tiết ca:
  - Tiền đầu ca.
  - Doanh thu tiền mặt.
  - Tiền thực đếm.
  - Chênh lệch.
  - Người mở/đóng.

## 18. Thanh Toán Và VietQR

### Mục tiêu

Ghi nhận thanh toán tiền mặt, chuyển khoản, ghi nợ và tự động khớp chuyển khoản qua webhook.

### API payment

- `POST /api/payments`
- `GET /api/payments`

### API VietQR/SePay

- `POST /vqr/api/token_generate`
- `POST /vqr/bank/api/test/transaction-callback`
- `POST /vqr/sepay/webhook`
- `GET /vqr/transactions`
- `GET /vqr/health`

### Luồng chuyển khoản

1. POS tạo đơn với mã đơn.
2. UI tạo QR chứa số tiền và nội dung chuyển khoản có mã đơn.
3. Webhook nhận giao dịch ngân hàng.
4. Backend extract order code từ nội dung.
5. Backend tìm đơn.
6. Nếu chưa thanh toán, tạo payment transaction.
7. Cập nhật paidAmount/debtAmount/status.
8. UI polling `/vqr/transactions` hoặc reload order để xác nhận.

### UI cần thiết kế

- QR rõ ràng, đủ lớn.
- Hiển thị số tiền, ngân hàng, số tài khoản, nội dung chuyển khoản.
- Trạng thái chờ tiền.
- Trạng thái đã khớp.
- Trạng thái chưa tìm thấy giao dịch.
- Nút xác nhận thủ công khi cần.

## 19. Admin Và Báo Cáo

### Mục tiêu

Quản trị hệ thống, báo cáo lợi nhuận, phân quyền, audit, thông báo và backup.

### Panels hiện có trong AdminView

1. Profit.
2. Permissions.
3. Audit.
4. Notifications.
5. Backups.

### Profit

Hiển thị:

- Daily profit.
- Monthly profit.
- Product profit.
- Revenue.
- Cost.
- Gross profit.
- Margin percent.

API:

- `GET /api/reports/profit/daily`
- `GET /api/reports/profit/monthly`
- `GET /api/reports/profit/products`

### Permissions

Hiển thị:

- Role selector.
- Permission groups.
- Checkbox quyền.
- Save permissions.

API:

- `GET /api/users/permissions`
- `GET /api/users/roles/{role}/permissions`
- `PUT /api/users/roles/{role}/permissions`

### Audit

Hiển thị:

- Filter serviceName.
- Filter severity.
- Filter action.
- Table logs:
  - time.
  - service.
  - action.
  - entity.
  - user.
  - severity.
  - description.

API:

- `GET /api/reports/activity-logs`

### Notifications

Hiển thị:

- Danh sách thông báo.
- Severity.
- Read/unread.
- Mark read.
- Mark all read.

API:

- `GET /api/notifications`
- `GET /api/notifications/unread-count`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`
- SignalR hub: `/hubs/notifications`

### Backups

Hiển thị:

- Danh sách backup.
- Trạng thái.
- Người tạo.
- Thời gian tạo.
- Thời gian restore.
- Note.
- Action restore.

API:

- `GET /api/backups`
- `POST /api/backups`
- `POST /api/backups/{backupId}/restore`

## 20. Quản Lý Người Dùng

### Mục tiêu

Admin quản lý tài khoản nhân viên.

### API

- `GET /api/users/me`
- `GET /api/users`
- `GET /api/users/{id}`
- `POST /api/users`
- `PUT /api/users/{id}`
- `PUT /api/users/{id}/toggle-active`
- `PUT /api/auth/change-password`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`

### UI cần có trong redesign

Hiện AdminView chưa có panel người dùng đầy đủ. Nên thêm:

- User list.
- Search/filter role.
- Create user.
- Edit profile.
- Activate/deactivate.
- Change password/self-service.
- Role badge.
- Permission summary.

## 21. Notification System

### Mục tiêu

Thông báo cho user về sự kiện cần chú ý như low stock, audit, hệ thống.

### Dữ liệu

- id.
- title.
- message.
- severity.
- link.
- isRead.
- createdAt.

### UI

- Badge unread count trên topbar.
- Dropdown preview 8 thông báo mới.
- Mark read từng thông báo.
- Mark all read.
- Full notification panel trong Admin.

### Trạng thái

- Không có thông báo.
- Có thông báo chưa đọc.
- SignalR mất kết nối, fallback polling 30 giây.

## 22. Event-Driven Backend

Các service dùng event để đồng bộ nghiệp vụ:

- `OrderCreatedEvent`
- `OrderReturnedEvent`
- `InventoryUpdatedEvent`
- `GoodsReceiptConfirmedEvent`
- `LowStockAlertEvent`
- `AuditLoggedEvent`

Ý nghĩa cho UI:

- UI nên refresh hoặc hiển thị notification khi kho thay đổi.
- Sau bán hàng, tồn kho và báo cáo có thể cập nhật qua event/backend.
- Sau nhập hàng, tồn kho và giá nhập được cập nhật.
- Sau trả hàng/hủy đơn, tồn kho được hoàn lại.

## 23. Data Model Cốt Lõi Cho Thiết Kế UI

### Product

- id
- sku/code
- barcode
- name
- category
- unit
- sellPrice
- importPrice
- quantityOnHand
- availableQuantity
- minThreshold
- maxThreshold
- alertLevel
- isActive

### Order

- id
- orderCode
- customer
- orderDate
- items
- subTotal
- discount
- promotion
- finalAmount
- paidAmount
- debtAmount
- paymentMethod
- status

### Inventory

- product
- quantityOnHand
- quantityReserved
- availableQuantity
- thresholds
- recommendedOrderQuantity
- transactions

### Goods Receipt

- receiptCode
- supplier
- status
- items
- totalAmount
- createdBy
- confirmedBy
- dates

### Customer

- code
- fullName
- phone
- group
- discount
- totalPurchased
- debtAmount

### Supplier

- code
- name
- contact
- address
- taxCode
- debtAmount

### User

- username
- fullName
- role
- permissions
- active status

## 24. Information Architecture Đề Xuất Cho UI Mới

### Admin

- Dashboard
- Sales
  - POS
  - Orders
  - Payments
  - Customers
  - Promotions
  - Shifts
- Inventory
  - Stock
  - Products
  - Goods Receipts
  - Stocktakes
  - Categories
  - Units
- Partners
  - Suppliers
  - Debt
- Reports
  - Revenue
  - Profit
  - Top products
  - Top customers
- System
  - Users
  - Permissions
  - Notifications
  - Audit logs
  - Backups

### Sales

- POS
- Orders
- Customers
- Debt
- Shift

### Warehouse

- Stock
- Products
- Goods Receipts
- Stocktakes
- Suppliers
- Supplier Debt

## 25. Các Màn Nên Code Lại Theo Thứ Tự

1. Login: sửa layout hỏng trước, centered auth form.
2. App Shell: role-based layout ổn định, responsive.
3. POS: màn quan trọng nhất cho Sales.
4. Inventory + Goods Receipt: màn quan trọng nhất cho Warehouse.
5. Orders: bảng, filter, action dialog.
6. Debt: tách customer debt và supplier debt rõ hơn.
7. Admin: chia panel thành module rõ ràng.
8. Product Management: bổ sung CRUD sản phẩm/barcode.
9. Customer/Supplier Management: bổ sung CRUD đầy đủ.
10. Promotions/Shifts: bổ sung màn quản trị còn thiếu.

## 26. Checklist UI/UX Khi Thiết Kế Lại

- Không dùng landing-page hero cho app nghiệp vụ.
- Không dùng chữ quá lớn trên màn thao tác.
- Không dùng glass/neon/cyber style cho POS/kho.
- Không dùng card lồng card nhiều tầng.
- Sidebar chỉ cho Admin.
- Sales/Warehouse dùng workspace tabs hoặc top navigation.
- Table phải dễ scan, row height ổn định.
- Các action nguy hiểm phải có confirm dialog.
- Button chính dùng cho hành động nghiệp vụ thật, không trang trí.
- Mỗi màn phải có loading, empty, error, success state.
- Mỗi form phải có validation, disabled state, loading state.
- Responsive phải ưu tiên thao tác thật trên tablet/mobile.
- Màu trạng thái thống nhất:
  - Success: paid, confirmed, safe stock.
  - Warning: draft, low stock, waiting payment.
  - Error: cancelled, out of stock, debt issue, failed.
  - Info: neutral/system state.

## 27. Ghi Chú Kỹ Thuật Cho Frontend Rebuild

Stack hiện tại:

- Vue 3.
- TypeScript.
- Vite.
- Vuetify.
- MDI icons.
- SignalR.
- html5-qrcode.

Nguyên tắc code lại UI:

- Giữ nguyên service layer trước khi chắc chắn cần đổi API.
- Tách component dùng chung:
  - `AppShell`
  - `PageHeader`
  - `MetricCard`
  - `DataToolbar`
  - `StatusChip`
  - `ConfirmDialog`
  - `EmptyState`
  - `MoneyText`
  - `ResponsiveDataTable`
  - `ProductSearch`
  - `BarcodeScannerDialog`
- Mỗi view nên có page-level state riêng.
- Không để toàn bộ logic POS nằm trong một file quá dài nếu code lại.
- Nên tách POS thành:
  - `PosCatalogPanel`
  - `PosCartPanel`
  - `ShiftPanel`
  - `CheckoutSummary`
  - `PaymentMethodSelector`
  - `ReceiptDialog`
  - `VietQrDialog`

## 28. Khoảng Trống Chức Năng Hiện Tại Nên Bổ Sung UI

Backend đã có nhưng UI chưa đầy đủ hoặc chưa tách rõ:

- Product CRUD.
- Category CRUD.
- Unit CRUD.
- Customer CRUD đầy đủ.
- Customer group CRUD.
- Supplier CRUD đầy đủ.
- Promotion CRUD.
- Shift history.
- User management.
- Change password.
- Payment history.
- Full reports dashboard.

Các phần này nên được đưa vào roadmap UI mới thay vì nhồi vào các màn hiện tại.

