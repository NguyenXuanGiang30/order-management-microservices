# Kế Hoạch Kết Nối API Và Gap Frontend RetailOps

## Kết Luận Hiện Trạng

Đúng, frontend hiện tại **chưa đủ giao diện và chức năng so với `docs/ui-redesign-functional-spec.md`**.

Frontend hiện đã có nhiều màn chính như login, dashboard, POS, orders, products, inventory, goods receipts, customers, suppliers, debts, promotions và settings. Tuy nhiên phần lớn các màn này đang dùng mock data từ `frontend/typescript-version/src/data/retailOps.ts`, chưa gọi API thật, và nhiều hành động nghiệp vụ trong tài liệu mới chỉ có nút/khung hiển thị chứ chưa có luồng xử lý.

Mục tiêu triển khai tiếp theo không phải viết lại giao diện, mà là:

- Giữ nguyên giao diện hiện có nhiều nhất có thể.
- Kết nối API thật qua API Gateway.
- Chỉ thêm UI nhỏ khi cần để thao tác API hoặc hiển thị trạng thái thiếu.
- Không redesign layout, sidebar, màu sắc, typography hoặc component nếu không bắt buộc.

## Nguyên Tắc Thực Hiện

- Không viết lại toàn bộ frontend.
- Không tự ý thay đổi thiết kế tổng thể.
- Không thay đổi navigation/theme/layout nếu không liên quan trực tiếp đến API.
- Ưu tiên thay mock data bằng API response thật.
- Thêm loading, error, empty, retry vì tài liệu yêu cầu và vì API có thể lỗi.
- Thêm dialog/form nhỏ nếu chức năng API bắt buộc cần chỗ nhập dữ liệu.
- Làm theo từng phase, kiểm tra typecheck/build sau mỗi nhóm thay đổi lớn.

## API Gateway

Frontend nên gọi qua API Gateway:

- Base URL mặc định: `http://localhost:5000`
- Có thể cấu hình bằng env, ví dụ: `VITE_API_BASE_URL=http://localhost:5000`

Các route backend chính:

- User & Report Service: `/api/auth`, `/api/users`, `/api/reports`, `/api/notifications`, `/api/backups`
- Product & Inventory Service: `/api/products`, `/api/categories`, `/api/units`, `/api/inventory`
- Order & Sales Service: `/api/orders`, `/api/customers`, `/api/customer-groups`, `/api/suppliers`, `/api/payments`, `/api/promotions`, `/api/shifts`, `/vqr`

## Đối Chiếu Theo Màn Hình

### 1. Login

Hiện trạng:

- Có form username/password.
- Có show/hide password.
- Submit hiện chỉ chuyển route sang `/dashboard`.
- Chưa gọi API login thật.
- Chưa lưu `accessToken`, `refreshToken`, `currentUser`.
- Chưa hiển thị lỗi 401/backend down theo API thật.

Thiếu so với tài liệu:

- `POST /api/auth/login`
- Loading submit.
- Error state từ API.
- Lưu session vào localStorage.
- Dùng token cho các request sau.
- Logout/refresh token.

Việc cần làm:

- Chỉ sửa logic submit trong `login.vue`.
- Giữ layout hiện tại.
- Thêm service auth tối thiểu.
- Lưu token/user vào localStorage.

### 2. App Shell / Navigation / Auth Guard

Hiện trạng:

- App đang có shell/sidebar sẵn.
- Topbar có notification dot tĩnh.
- User profile đang là dữ liệu tĩnh.
- Chưa có guard theo token/role.

Thiếu so với tài liệu:

- Guard khi chưa đăng nhập.
- Guard khi không có quyền.
- Hiển thị tên user/role thật.
- Notification unread count thật.
- Loading/error backend ở app shell.
- Role-based navigation Admin/Sales/Warehouse.

Việc cần làm:

- Ưu tiên auth guard tối thiểu trước.
- Chưa đổi navigation nếu chưa cần.
- Có thể cập nhật UserProfile lấy user thật từ localStorage/API.
- Notification topbar làm sau phase dữ liệu chính.

### 3. Dashboard

Hiện trạng:

- Có KPI cards, chart, cảnh báo tồn kho, hoạt động gần đây.
- Dữ liệu lấy từ mock `retailOps.ts`.

Thiếu so với tài liệu:

- `GET /api/reports/dashboard`
- Có thể dùng thêm `/api/reports/daily`, `/api/reports/monthly`, `/api/reports/top-products`, `/api/reports/top-customers`
- Loading/empty/error/retry.
- Dữ liệu hoạt động gần đây từ API thật.

Việc cần làm:

- Nối `GET /api/reports/dashboard`.
- Nếu API dashboard chưa đủ low stock/recent activities thì fallback tổng hợp từ products, inventory, orders, receipts đã load.
- Không đổi layout dashboard.

### 4. POS

Hiện trạng:

- Có catalog sản phẩm, cart panel, chọn khách hàng, chọn khuyến mãi, chọn payment method.
- Cart hiện là dữ liệu khởi tạo từ mock.
- Nút thêm sản phẩm chưa có xử lý API thật.
- Checkout chưa tạo order/payment thật.

Thiếu so với tài liệu:

- `GET /api/shifts/current`
- `POST /api/shifts/open`
- `POST /api/shifts/{id}/close`
- `GET /api/products`
- `GET /api/inventory/stock`
- `GET /api/customers`
- `GET /api/promotions/active`
- `GET /api/promotions/{code}/validate`
- `POST /api/orders`
- `POST /api/payments`
- `GET /vqr/transactions?orderCode=...`
- Trạng thái chưa mở ca, đang checkout, checkout thành công, chờ chuyển khoản, ghi nợ thiếu khách hàng.

Việc cần làm:

- Giữ layout POS hiện tại.
- Thay products/customers/promotions mock bằng API thật.
- Làm cart local state.
- Khi checkout gọi `POST /api/orders`.
- Nếu cash/transfer/partial debt thì gọi `POST /api/payments` theo response order.
- Thêm dialog/alert nhỏ cho kết quả checkout và lỗi API.
- Shift/VietQR nối sau khi order flow ổn.

### 5. Orders

Hiện trạng:

- Có search/filter UI cơ bản.
- Có table đơn hàng.
- Dữ liệu lấy từ mock.
- Chưa có detail/cancel/return/payment action thật.

Thiếu so với tài liệu:

- `GET /api/orders`
- `GET /api/orders/{id}`
- `PUT /api/orders/{id}/cancel`
- `POST /api/orders/{id}/return`
- `GET /api/orders/{id}/invoice`
- `GET /api/orders/{id}/status-history`
- `POST /api/payments`
- Dialog chi tiết hóa đơn.
- Confirm dialog hủy/trả hàng.
- Action loading theo từng dòng.

Việc cần làm:

- Phase đầu: nối `GET /api/orders`.
- Map `OrderDto` sang table hiện tại.
- Thêm loading/error/empty/retry.
- Phase sau: thêm detail dialog và action cancel/return/payment.

### 6. Products

Hiện trạng:

- Có product table.
- Có search/category UI.
- Có nút import/thêm sản phẩm nhưng chưa xử lý.
- Dữ liệu lấy từ mock.

Thiếu so với tài liệu:

- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products`
- `PUT /api/products/{id}`
- `PUT /api/products/{id}/toggle-active`
- `GET /api/categories`
- `GET /api/units`
- Form tạo/sửa sản phẩm.
- Toggle active.
- Cảnh báo thiếu barcode.

Việc cần làm:

- Phase đầu: nối `GET /api/products`.
- Giữ table hiện tại.
- Phase sau: thêm dialog tạo/sửa sản phẩm tối thiểu.
- Nối categories/units để select trong form.
- Nối toggle active nếu thêm action vào row.

### 7. Inventory

Hiện trạng:

- Có KPI tồn kho và table.
- Dữ liệu lấy từ mock products.
- Chưa gọi stock API.
- Chưa có stocktake/transactions thật.

Thiếu so với tài liệu:

- `GET /api/inventory/stock`
- `GET /api/inventory/transactions`
- Export stock/transactions.
- Stocktake list/detail/create/update/confirm/cancel/import/template.
- Filter cảnh báo.
- Empty/error/loading.

Việc cần làm:

- Phase đầu: nối `GET /api/inventory/stock`.
- Map `StockDto` sang table hiện tại.
- Thêm search/belowMin query nếu dùng input/filter hiện có.
- Phase sau: thêm stocktake dialog/list tối thiểu.

### 8. Goods Receipts

Hiện trạng:

- Có table phiếu nhập.
- Có panel nhập nhanh barcode.
- Dữ liệu lấy từ mock.
- Nút tạo phiếu nhập chưa xử lý API thật.

Thiếu so với tài liệu:

- `GET /api/inventory/receipts`
- `POST /api/inventory/receipts`
- `PUT /api/inventory/receipts/{id}/confirm`
- `PUT /api/inventory/receipts/{id}/cancel`
- `GET /api/suppliers`
- `GET /api/products`
- Dialog tạo phiếu nhập nhiều dòng.
- Detail dialog.
- Confirm/cancel action.

Việc cần làm:

- Phase đầu: nối `GET /api/inventory/receipts`.
- Phase sau: thêm dialog tạo phiếu nhập tối thiểu.
- Dùng API suppliers/products cho select.
- Thêm confirm/cancel action trên row.

### 9. Customers

Hiện trạng:

- Có customer table.
- Dữ liệu lấy từ mock.
- Nút thêm khách hàng chưa xử lý.

Thiếu so với tài liệu:

- `GET /api/customers`
- `GET /api/customers/{id}`
- `POST /api/customers`
- `PUT /api/customers/{id}`
- `GET /api/customers/{id}/debt`
- Customer groups.
- Detail/purchase history/debt view.

Việc cần làm:

- Phase đầu: nối `GET /api/customers`.
- Phase sau: thêm create/edit dialog đơn giản.
- Thêm detail/debt dialog nếu cần cho debt flow.

### 10. Suppliers

Hiện trạng:

- Có supplier table.
- Dữ liệu lấy từ mock.
- Nút thêm nhà cung cấp chưa xử lý.

Thiếu so với tài liệu:

- `GET /api/suppliers`
- `POST /api/suppliers`
- `PUT /api/suppliers/{id}`
- Supplier detail.
- Supplier debt.
- Danh sách phiếu nhập theo supplier.

Việc cần làm:

- Phase đầu: nối `GET /api/suppliers`.
- Phase sau: thêm create/edit dialog đơn giản.
- Công nợ supplier có thể hiển thị nếu backend DTO hỗ trợ hoặc từ receipt/payment data.

### 11. Debts

Hiện trạng:

- Có 2 danh sách công nợ khách hàng và nhà cung cấp.
- Dữ liệu lấy từ mock customers/suppliers.
- Chưa có thanh toán công nợ thật.

Thiếu so với tài liệu:

- `GET /api/customers`
- `GET /api/customers/{id}/debt`
- `GET /api/suppliers`
- `POST /api/payments`
- Dialog nhập số tiền thanh toán.

Việc cần làm:

- Nối customers/suppliers API.
- Với khách hàng: dùng `GET /api/customers/{id}/debt`.
- Thêm dialog thanh toán nhỏ gọi `POST /api/payments`.
- Supplier debt API hiện chưa rõ endpoint riêng, cần kiểm tra thêm nếu triển khai sâu.

### 12. Promotions

Hiện trạng:

- Có promotion table.
- Dữ liệu lấy từ mock.
- Nút tạo promotion chưa xử lý.

Thiếu so với tài liệu:

- `GET /api/promotions/active`
- `GET /api/promotions/{code}/validate`
- `POST /api/promotions`
- `PUT /api/promotions/{id}`
- `PUT /api/promotions/{id}/toggle-active`
- Form tạo/sửa promotion.

Việc cần làm:

- Phase đầu: nối `GET /api/promotions/active`.
- Phase sau: thêm create/edit/toggle nếu cần admin quản lý promotion.

### 13. Settings / Admin

Hiện trạng:

- Có role table và store config tĩnh.
- Chưa có admin panels thật.

Thiếu so với tài liệu:

- Users management.
- Permissions management.
- Notifications panel.
- Audit logs.
- Backups.
- Change password.
- Profit reports.

API tương ứng:

- `GET /api/users`
- `POST /api/users`
- `PUT /api/users/{id}`
- `PUT /api/users/{id}/toggle-active`
- `GET /api/users/permissions`
- `GET /api/users/roles/{role}/permissions`
- `PUT /api/users/roles/{role}/permissions`
- `GET /api/notifications`
- `GET /api/notifications/unread-count`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`
- `GET /api/reports/activity-logs`
- `GET /api/backups`
- `POST /api/backups`
- `POST /api/backups/{backupId}/restore`
- `GET /api/reports/profit/daily`
- `GET /api/reports/profit/monthly`
- `GET /api/reports/profit/products`

Việc cần làm:

- Làm sau các màn vận hành chính.
- Thêm tabs/panels tối thiểu trong `settings.vue`.
- Không redesign settings toàn bộ nếu không cần.

## Thứ Tự Triển Khai Đề Xuất

### Phase 1: API Client Và Auth

File dự kiến:

- `frontend/typescript-version/src/services/api.ts`
- `frontend/typescript-version/src/services/authApi.ts`
- `frontend/typescript-version/src/pages/login.vue`

Công việc:

- Tạo API client dùng `fetch`.
- Thêm base URL qua env.
- Parse response chuẩn `{ success, data, message, errors }`.
- Gắn Authorization header.
- Login thật qua `/api/auth/login`.
- Lưu token/user.
- Hiển thị loading/error trong login.

### Phase 2: Nối GET Data Cho Các Bảng Chính

File dự kiến:

- `src/pages/products.vue`
- `src/pages/inventory.vue`
- `src/pages/orders.vue`
- `src/pages/customers.vue`
- `src/pages/suppliers.vue`
- `src/pages/goods-receipts.vue`
- `src/pages/promotions.vue`

Công việc:

- Thay mock imports bằng API calls.
- Map DTO backend sang field UI hiện tại.
- Thêm loading/error/empty/retry.
- Không đổi layout table.

### Phase 3: Dashboard Và POS Read Data

File dự kiến:

- `src/pages/dashboard.vue`
- `src/pages/pos.vue`

Công việc:

- Dashboard gọi reports API và/hoặc tổng hợp từ API đã có.
- POS load products, customers, active promotions, current shift.
- Cart vẫn xử lý local state.

### Phase 4: Action API Chính

File dự kiến:

- `src/pages/pos.vue`
- `src/pages/goods-receipts.vue`
- `src/pages/orders.vue`
- `src/pages/debts.vue`

Công việc:

- POS tạo order qua `POST /api/orders`.
- POS/payment gọi `POST /api/payments`.
- Goods receipt create/confirm/cancel.
- Orders detail/cancel/return/payment.
- Debt settlement dialog gọi payment API.

### Phase 5: CRUD Bổ Sung

File dự kiến:

- `src/pages/products.vue`
- `src/pages/customers.vue`
- `src/pages/suppliers.vue`
- `src/pages/promotions.vue`

Công việc:

- Thêm dialog tạo/sửa nhỏ.
- Nối POST/PUT/toggle.
- Dùng categories/units/customer-groups khi cần.

### Phase 6: Admin/Settings Và Notifications

File dự kiến:

- `src/pages/settings.vue`
- `src/layouts/components/DefaultLayoutWithVerticalNav.vue`
- `src/layouts/components/UserProfile.vue`

Công việc:

- User management.
- Permissions.
- Notifications unread count/preview.
- Audit logs.
- Backups.
- Profit reports.

## Kiểm Tra Sau Khi Làm

Sau mỗi phase:

- Chạy `npm run typecheck`.
- Khi hoàn tất nhóm lớn, chạy `npm run build`.

Smoke test thủ công:

- Login thành công bằng API thật.
- Token được gửi trong request.
- Products load từ backend.
- Inventory load từ backend.
- Orders load từ backend.
- Customers/Suppliers load từ backend.
- POS tạo đơn được.
- Goods receipt tạo/xác nhận/hủy được.
- Backend down không làm trắng màn.

## File Dự Kiến Thêm

- `frontend/typescript-version/src/services/api.ts`
- `frontend/typescript-version/src/services/authApi.ts`
- `frontend/typescript-version/src/services/retailApi.ts`
- `frontend/typescript-version/src/types/api.ts`

## File Dự Kiến Sửa

- `frontend/typescript-version/src/pages/login.vue`
- `frontend/typescript-version/src/pages/dashboard.vue`
- `frontend/typescript-version/src/pages/pos.vue`
- `frontend/typescript-version/src/pages/orders.vue`
- `frontend/typescript-version/src/pages/products.vue`
- `frontend/typescript-version/src/pages/inventory.vue`
- `frontend/typescript-version/src/pages/goods-receipts.vue`
- `frontend/typescript-version/src/pages/customers.vue`
- `frontend/typescript-version/src/pages/suppliers.vue`
- `frontend/typescript-version/src/pages/debts.vue`
- `frontend/typescript-version/src/pages/promotions.vue`
- `frontend/typescript-version/src/pages/settings.vue`

## Phần Chưa Nên Làm Ngay

Các phần sau nên để sau khi API nền đã nối ổn:

- Redesign role-based shell.
- Tách POS thành nhiều component con.
- SignalR notification realtime.
- Full stocktake import/export UI.
- Full reports dashboard.
- Backup/restore workflow sâu.
- VietQR QR image generation nếu chưa có cấu hình ngân hàng rõ.

## Kết Luận

Frontend hiện tại có khung giao diện cho nhiều màn, nhưng chưa đủ so với tài liệu vì:

- Chưa kết nối API thật.
- Thiếu auth/session/token.
- Thiếu loading/error/empty state ở nhiều màn.
- Thiếu nhiều action nghiệp vụ.
- Thiếu một số panel admin và CRUD.

Hướng triển khai đúng là nối API từng bước, giữ nguyên giao diện hiện tại, chỉ bổ sung phần còn thiếu khi API flow yêu cầu.
