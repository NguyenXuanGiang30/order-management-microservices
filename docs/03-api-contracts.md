# Thiết kế API (API Contracts)

Tài liệu này định nghĩa các RESTful API chính cho 3 microservices.
Tất cả request từ Frontend đều đi qua **Ocelot API Gateway** (port `5000`). Gateway xác thực JWT Token trước khi forward request đến service tương ứng.

> **Quy ước chung:**
> - Tất cả response thành công trả về dạng: `{ "success": true, "data": {...}, "message": "..." }`
> - Tất cả response lỗi trả về dạng: `{ "success": false, "errors": [...], "message": "..." }`
> - Phân trang sử dụng query params: `?page=1&pageSize=20`
> - Response phân trang: `{ "data": [...], "totalCount": 100, "page": 1, "pageSize": 20, "totalPages": 5 }`

---

## 1. User & Report Service (Port: 5003)

**Gateway Route:** `/api/auth/*`, `/api/users/*`, `/api/reports/*`

### 1.1 Xác thực (Authentication)

#### `POST /api/auth/login` — Đăng nhập
- **Quyền:** Public
- **Request Body:**
  ```json
  {
    "username": "admin",
    "password": "password123"
  }
  ```
- **Response 200:**
  ```json
  {
    "success": true,
    "data": {
      "accessToken": "eyJhbGciOi...",
      "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
      "expiresIn": 3600,
      "user": {
        "id": "guid-001",
        "username": "admin",
        "fullName": "Nguyễn Admin",
        "role": "Admin",
        "avatarUrl": "/avatars/admin.jpg"
      }
    }
  }
  ```

#### `POST /api/auth/refresh-token` — Cấp lại Access Token
- **Quyền:** Public (cần Refresh Token hợp lệ)
- **Request Body:**
  ```json
  {
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
  }
  ```
- **Response 200:**
  ```json
  {
    "success": true,
    "data": {
      "accessToken": "eyJhbGciOi...(mới)",
      "refreshToken": "cmVmcmVzaC10b2tlbi1tb2k=...(mới)",
      "expiresIn": 3600
    }
  }
  ```

#### `POST /api/auth/logout` — Đăng xuất (Revoke Refresh Token)
- **Quyền:** Authenticated
- **Request Body:**
  ```json
  {
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
  }
  ```
- **Response 200:** `{ "success": true, "message": "Đăng xuất thành công" }`

#### `PUT /api/auth/change-password` — Đổi mật khẩu
- **Quyền:** Authenticated
- **Request Body:**
  ```json
  {
    "oldPassword": "CurrentPassword123",
    "newPassword": "NewSuperSecurePassword456"
  }
  ```
- **Response 200:** `{ "success": true, "message": "Đổi mật khẩu thành công" }`

#### `GET /api/users/me` — Xem thông tin cá nhân
- **Quyền:** Authenticated
- **Response 200:**
  ```json
  {
    "success": true,
    "data": {
      "id": "guid-001",
      "username": "admin",
      "fullName": "Nguyễn Admin",
      "email": "admin@company.com",
      "phone": "0987654321",
      "role": "Admin",
      "avatarUrl": "/avatars/admin.jpg"
    }
  }
  ```

### 1.2 Quản lý Người dùng

#### `GET /api/users` — Danh sách người dùng
- **Quyền:** Admin
- **Query Params:** `?page=1&pageSize=20&role=Sales&search=nguyen`
- **Response 200:**
  ```json
  {
    "success": true,
    "data": [
      {
        "id": "guid-001",
        "username": "nvsales01",
        "fullName": "Nguyễn Văn Sales",
        "role": "Sales",
        "isActive": true,
        "lastLoginAt": "2023-10-01T08:00:00Z"
      }
    ],
    "totalCount": 15,
    "page": 1,
    "pageSize": 20
  }
  ```

#### `POST /api/users` — Tạo tài khoản mới
- **Quyền:** Admin
- **Request Body:**
  ```json
  {
    "username": "nvkho01",
    "password": "Abc@12345",
    "fullName": "Trần Thủ Kho",
    "email": "kho01@company.com",
    "phone": "0901234567",
    "role": "Warehouse"
  }
  ```

#### `PUT /api/users/{id}` — Cập nhật thông tin
- **Quyền:** Admin (cập nhật bất kỳ), hoặc chính user đó (cập nhật profile của mình)

#### `PUT /api/users/{id}/toggle-active` — Kích hoạt / Vô hiệu hóa tài khoản
- **Quyền:** Admin

### 1.3 Báo cáo & Dashboard

#### `GET /api/reports/dashboard` — Dữ liệu Dashboard tổng quan
- **Quyền:** Admin
- **Response 200:**
  ```json
  {
    "success": true,
    "data": {
      "today": { "totalOrders": 25, "totalRevenue": 15000000 },
      "thisMonth": { "totalOrders": 680, "totalRevenue": 450000000, "totalNewCustomers": 32 },
      "revenueChart": [
        { "date": "2023-10-01", "revenue": 15000000 },
        { "date": "2023-10-02", "revenue": 18500000 }
      ]
    }
  }
  ```

#### `GET /api/reports/top-products` — Top sản phẩm bán chạy
- **Quyền:** Admin
- **Query Params:** `?year=2023&month=10&limit=10`
- **Response 200:**
  ```json
  {
    "success": true,
    "data": [
      {
        "productId": "prod-1",
        "productCode": "SKU001",
        "productName": "Laptop Dell XPS",
        "totalQuantitySold": 150,
        "totalRevenue": 3750000000
      }
    ]
  }
  ```

#### `GET /api/reports/top-customers` — Top khách hàng theo doanh số
- **Quyền:** Admin
- **Query Params:** `?year=2023&month=10&limit=10`

#### `GET /api/reports/revenue-by-month` — Biểu đồ doanh thu theo tháng
- **Quyền:** Admin
- **Query Params:** `?year=2023`

#### `GET /api/reports/activity-logs` — Nhật ký hoạt động
- **Quyền:** Admin
- **Query Params:** `?userId=guid-001&from=2023-10-01&to=2023-10-31&page=1`

---

## 2. Product & Inventory Service (Port: 5001)

**Gateway Route:** `/api/products/*`, `/api/categories/*`, `/api/inventory/*`, `/api/units/*`

### 2.1 Danh mục sản phẩm

#### `GET /api/categories` — Lấy cây danh mục
- **Quyền:** Admin, Sales, Warehouse
- **Response 200:** Trả về danh mục dạng cây (nested children).
  ```json
  {
    "success": true,
    "data": [
      {
        "id": "cat-1",
        "name": "Điện tử",
        "children": [
          { "id": "cat-2", "name": "Laptop", "children": [] },
          { "id": "cat-3", "name": "Phụ kiện", "children": [] }
        ]
      }
    ]
  }
  ```

#### `POST /api/categories` — Tạo danh mục
- **Quyền:** Admin, Warehouse
- **Request Body:** `{ "name": "Laptop Gaming", "parentId": "cat-2" }`

#### `PUT /api/categories/{id}` — Cập nhật danh mục
#### `DELETE /api/categories/{id}` — Xóa danh mục (soft-delete)

### 2.2 Đơn vị tính

#### `GET /api/units` — Danh sách đơn vị tính
- **Quyền:** Admin, Sales, Warehouse
#### `POST /api/units` — Tạo đơn vị tính mới
- **Quyền:** Admin, Warehouse
- **Request Body:** `{ "name": "Kilogram", "abbreviation": "kg" }`

### 2.3 Sản phẩm

#### `GET /api/products` — Danh sách sản phẩm
- **Quyền:** Admin, Sales, Warehouse
- **Query Params:** `?categoryId=cat-2&search=dell&isActive=true&page=1&pageSize=20`
- **Response 200:**
  ```json
  {
    "success": true,
    "data": [
      {
        "id": "prod-1",
        "code": "SKU001",
        "name": "Laptop Dell XPS 15",
        "categoryName": "Laptop",
        "unitName": "Cái",
        "sellPrice": 25000000,
        "quantityOnHand": 10,
        "minThreshold": 5,
        "isActive": true,
        "imageUrl": "/images/prod-1.jpg"
      }
    ],
    "totalCount": 150
  }
  ```

#### `GET /api/products/{id}` — Chi tiết sản phẩm
#### `POST /api/products` — Tạo sản phẩm mới
- **Quyền:** Admin, Warehouse
- **Request Body:**
  ```json
  {
    "code": "SKU002",
    "name": "Chuột Logitech MX Master",
    "description": "Chuột không dây cao cấp",
    "barcode": "8934567890123",
    "importPrice": 1200000,
    "sellPrice": 1800000,
    "categoryId": "cat-3",
    "unitId": "unit-1",
    "minThreshold": 10,
    "maxThreshold": 200
  }
  ```

#### `PUT /api/products/{id}` — Cập nhật sản phẩm
#### `PUT /api/products/{id}/toggle-active` — Ngừng bán / Mở bán lại

### 2.4 Nhập kho

#### `GET /api/inventory/receipts` — Danh sách phiếu nhập
- **Quyền:** Admin, Warehouse
- **Query Params:** `?status=Confirmed&from=2023-10-01&to=2023-10-31&page=1`

#### `POST /api/inventory/receipts` — Tạo phiếu nhập (Draft)
- **Quyền:** Admin, Warehouse
- **Request Body:**
  ```json
  {
    "supplierId": "sup-1",
    "note": "Nhập lô hàng tháng 10",
    "items": [
      { "productId": "prod-1", "quantity": 50, "unitPrice": 20000000 },
      { "productId": "prod-2", "quantity": 100, "unitPrice": 1200000 }
    ]
  }
  ```

#### `PUT /api/inventory/receipts/{id}/confirm` — Xác nhận phiếu nhập
- **Quyền:** Admin, Warehouse
- **Mô tả:** Chuyển trạng thái `Draft → Confirmed`, cập nhật tồn kho, ghi `InventoryTransaction`, publish event `stock.updated`.

#### `PUT /api/inventory/receipts/{id}/cancel` — Hủy phiếu nhập

### 2.5 Tồn kho & Cảnh báo

#### `GET /api/inventory/stock` — Xem tồn kho
- **Quyền:** Admin, Warehouse
- **Query Params:** `?belowMin=true` (lọc sản phẩm dưới ngưỡng tối thiểu)

#### `GET /api/inventory/transactions` — Lịch sử biến động kho
- **Quyền:** Admin, Warehouse
- **Query Params:** `?productId=prod-1&type=Import&from=2023-10-01&page=1`

### 2.6 API Nội bộ (Internal — Không qua Gateway)

> Các API này chỉ dùng cho giao tiếp đồng bộ giữa các service (Service-to-Service). Không expose ra ngoài Gateway.

#### `GET /internal/products/{id}/price-check` — Kiểm tra giá và tồn kho
- **Gọi bởi:** Order & Sales Service (khi tạo đơn hàng)
- **Response 200:**
  ```json
  {
    "id": "prod-1",
    "code": "SKU001",
    "name": "Laptop Dell XPS 15",
    "unitName": "Cái",
    "sellPrice": 25000000,
    "quantityOnHand": 10,
    "isAvailable": true
  }
  ```

#### `POST /internal/inventory/reserve` — Giữ hàng cho đơn
- **Gọi bởi:** Order & Sales Service
- **Request Body:**
  ```json
  {
    "items": [
      { "productId": "prod-1", "quantity": 2 }
    ]
  }
  ```

#### `POST /internal/inventory/deduct` — Trừ kho sau khi thanh toán
- **Gọi bởi:** Order & Sales Service (hoặc xử lý qua event `order.created`)

---

## 3. Order & Sales Service (Port: 5002)

**Gateway Route:** `/api/orders/*`, `/api/customers/*`, `/api/suppliers/*`, `/api/payments/*`

### 3.1 Khách hàng

#### `GET /api/customer-groups` — Lấy danh sách nhóm khách hàng
- **Quyền:** Admin, Sales
- **Response 200:**
  ```json
  {
    "success": true,
    "data": [
      {
        "id": "group-vip-id",
        "name": "Khách VIP",
        "defaultDiscountPercent": 10.0,
        "note": "Khách hàng thân thiết mua nhiều"
      },
      {
        "id": "group-retail-id",
        "name": "Khách Lẻ",
        "defaultDiscountPercent": 0.0,
        "note": "Khách hàng phổ thông"
      }
    ]
  }
  ```

#### `POST /api/customer-groups` — Tạo nhóm khách hàng mới
- **Quyền:** Admin
- **Request Body:**
  ```json
  {
    "name": "Khách Sỉ",
    "defaultDiscountPercent": 15.0,
    "note": "Đại lý phân phối cấp 2"
  }
  ```
- **Response 201:** `{ "success": true, "data": { "id": "group-wholesale-id", ... } }`

#### `PUT /api/customer-groups/{id}` — Cập nhật nhóm khách hàng
- **Quyền:** Admin
- **Request Body:**
  ```json
  {
    "name": "Khách Sỉ Nâng Cấp",
    "defaultDiscountPercent": 18.0,
    "note": "Cập nhật chiết khấu đại lý sỉ"
  }
  ```
- **Response 200:** `{ "success": true, "message": "Cập nhật nhóm khách hàng thành công" }`

#### `DELETE /api/customer-groups/{id}` — Xóa nhóm khách hàng
- **Quyền:** Admin
- **Response 200:** `{ "success": true, "message": "Xóa nhóm khách hàng thành công" }`

#### `GET /api/customers` — Danh sách khách hàng
- **Quyền:** Admin, Sales
- **Query Params:** `?search=nguyen&type=Individual&page=1&pageSize=20`

#### `GET /api/customers/{id}` — Chi tiết khách hàng (kèm lịch sử mua)
- **Quyền:** Admin, Sales
- **Response 200:**
  ```json
  {
    "success": true,
    "data": {
      "id": "cust-1",
      "code": "KH001",
      "fullName": "Nguyễn Văn A",
      "phone": "0901234567",
      "email": "nva@email.com",
      "customerGroupId": "group-vip-id",
      "customerGroupName": "Khách VIP",
      "totalPurchased": 75000000,
      "debtAmount": 5000000,
      "recentOrders": [
        { "orderId": "ord-1", "orderCode": "ORD-20231001-001", "finalAmount": 25000000, "status": "Paid" }
      ]
    }
  }
  ```

#### `POST /api/customers` — Tạo khách hàng mới
#### `PUT /api/customers/{id}` — Cập nhật thông tin khách hàng

### 3.2 Nhà cung cấp

#### `GET /api/suppliers` — Danh sách nhà cung cấp
- **Quyền:** Admin, Warehouse
#### `POST /api/suppliers` — Tạo nhà cung cấp
#### `PUT /api/suppliers/{id}` — Cập nhật nhà cung cấp

### 3.3 Đơn hàng

#### `GET /api/orders` — Danh sách đơn hàng
- **Quyền:** Admin, Sales
- **Query Params:** `?status=Paid&customerId=cust-1&from=2023-10-01&to=2023-10-31&page=1`

#### `GET /api/orders/{id}` — Chi tiết đơn hàng
- **Quyền:** Admin, Sales

#### `POST /api/orders` — Tạo đơn hàng mới
- **Quyền:** Admin, Sales
- **Luồng xử lý:**
  1. Gọi nội bộ `Product & Inventory Service /internal/products/{id}/price-check` để lấy giá và kiểm tra tồn kho.
  2. Tính `SubTotal`, áp dụng chiết khấu → `FinalAmount`.
  3. Lưu đơn hàng + snapshot thông tin sản phẩm vào `OrderDetail`.
  4. Cập nhật `Customer.DebtAmount` nếu chưa thanh toán đủ.
  5. Publish event `order.created` ra RabbitMQ.
- **Request Body:**
  ```json
  {
    "customerId": "cust-1",
    "discountPercent": 5,
    "paymentMethod": "Cash",
    "paidAmount": 24500000,
    "note": "Khách quen giảm 5%",
    "items": [
      { "productId": "prod-1", "quantity": 1 },
      { "productId": "prod-2", "quantity": 2 }
    ]
  }
  ```
- **Response 201:**
  ```json
  {
    "success": true,
    "data": {
      "orderId": "ord-1",
      "orderCode": "ORD-20231001-001",
      "subTotal": 27400000,
      "discountAmount": 1370000,
      "finalAmount": 26030000,
      "paidAmount": 24500000,
      "debtAmount": 1530000,
      "status": "PartialPaid"
    }
  }
  ```

#### `PUT /api/orders/{id}` — Sửa đơn hàng (khi Pending)
- **Quyền:** Admin, Sales
- **Mô tả:** Thêm/bớt sản phẩm, sửa chiết khấu khi đơn hàng chưa thanh toán và chưa xác nhận. Ghi log vào `OrderStatusHistory`.

#### `PUT /api/orders/{id}/cancel` — Hủy đơn hàng
- **Quyền:** Admin
- **Mô tả:** Chuyển trạng thái sang `Cancelled`, ghi log vào `OrderStatusHistory`, cập nhật công nợ và hoàn tồn kho.

#### `POST /api/orders/{id}/return` — Khách trả hàng
- **Quyền:** Admin, Sales
- **Mô tả:** Xử lý trả hàng toàn bộ hoặc một phần.
  1. Tính toán lại `TotalRefundAmount`.
  2. Tạo bản ghi trong `ReturnOrder` và `ReturnOrderDetail`.
  3. Cập nhật `Order.Status = Returned` (nếu trả toàn bộ).
  4. Trừ `DebtAmount` của khách hàng hoặc ghi nhận hoàn tiền.
  5. Publish event `order.returned`.
- **Request Body:**
  ```json
  {
    "returnReason": "Khách đổi ý",
    "items": [
      { "orderDetailId": "item-1", "returnQuantity": 1 }
    ]
  }
  ```

#### `GET /api/orders/{id}/invoice` — Xuất hóa đơn
- **Quyền:** Admin, Sales
- **Mô tả:** Trả về dữ liệu chuẩn để Frontend render hóa đơn in nhiệt hoặc file PDF.

#### `GET /api/orders/{id}/status-history` — Xem lịch sử trạng thái đơn hàng
- **Quyền:** Admin, Sales
- **Response 200:**
  ```json
  {
    "success": true,
    "data": [
      {
        "id": "history-1",
        "orderId": "ord-1",
        "fromStatus": "Pending",
        "toStatus": "PartialPaid",
        "reason": "Khách thanh toán đợt 1",
        "changedBy": "guid-sales-01",
        "changedByName": "Nhân viên Sales 01",
        "changedAt": "2023-10-01T10:15:00Z"
      }
    ]
  }
  ```

### 3.4 Thanh toán & Công nợ

#### `POST /api/payments` — Ghi nhận thanh toán
- **Quyền:** Admin, Sales
- **Mô tả:** Ghi nhận một khoản thanh toán cho đơn hàng. Cập nhật `Order.PaidAmount`, `Order.DebtAmount` và `Customer.DebtAmount`.
- **Request Body:**
  ```json
  {
    "orderId": "ord-1",
    "customerId": "cust-1",
    "amount": 1530000,
    "paymentMethod": "BankTransfer",
    "note": "Trả nợ đợt 2"
  }
  ```

#### `GET /api/payments` — Lịch sử thanh toán
- **Quyền:** Admin, Sales
- **Query Params:** `?customerId=cust-1&orderId=ord-1&from=2023-10-01&page=1`

#### `GET /api/customers/{id}/debt` — Xem chi tiết công nợ của khách hàng
- **Quyền:** Admin, Sales
- **Response 200:**
  ```json
  {
    "success": true,
    "data": {
      "customerId": "cust-1",
      "customerName": "Nguyễn Văn A",
      "totalDebt": 5000000,
      "unpaidOrders": [
        { "orderCode": "ORD-20231001-001", "finalAmount": 26030000, "paidAmount": 24500000, "debtAmount": 1530000 }
      ]
    }
  }
  ```

---

## 4. Chi tiết các Mã lỗi HTTP (Error Response Examples)

Tất cả các API của hệ thống đều tuân thủ định dạng trả về lỗi nhất quán khi gặp sự cố:

### 4.1 Lỗi 400 Bad Request (Dữ liệu đầu vào không hợp lệ)
```json
{
  "success": false,
  "message": "Dữ liệu yêu cầu không hợp lệ hoặc thiếu trường bắt buộc",
  "errors": [
    {
      "field": "fullName",
      "message": "Tên khách hàng không được để trống"
    },
    {
      "field": "phone",
      "message": "Số điện thoại không đúng định dạng (phải có 10 chữ số)"
    }
  ]
}
```

### 4.2 Lỗi 401 Unauthorized (Chưa xác thực hoặc token hết hạn)
```json
{
  "success": false,
  "message": "Token xác thực không hợp lệ hoặc đã hết hạn",
  "errors": [
    {
      "code": "AUTH_TOKEN_EXPIRED",
      "message": "Access token has expired. Please refresh token."
    }
  ]
}
```

### 4.3 Lỗi 403 Forbidden (Không có quyền truy cập)
```json
{
  "success": false,
  "message": "Bạn không có quyền truy cập vào tài nguyên này",
  "errors": [
    {
      "code": "ACCESS_DENIED",
      "message": "Yêu cầu quyền Admin nhưng tài khoản hiện tại là Sales"
    }
  ]
}
```

### 4.4 Lỗi 404 Not Found (Tài nguyên không tồn tại)
```json
{
  "success": false,
  "message": "Không tìm thấy dữ liệu yêu cầu",
  "errors": [
    {
      "code": "PRODUCT_NOT_FOUND",
      "message": "Không tìm thấy sản phẩm có ID 'prod-999'"
    }
  ]
}
```

### 4.5 Lỗi 409 Conflict (Xung đột nghiệp vụ / Dữ liệu đã tồn tại)
```json
{
  "success": false,
  "message": "Có sự xung đột hoặc vi phạm ràng buộc dữ liệu",
  "errors": [
    {
      "code": "PRODUCT_CODE_DUPLICATED",
      "message": "Mã sản phẩm SKU001 đã tồn tại trong hệ thống"
    }
  ]
}
```

### 4.6 Lỗi 500 Internal Server Error (Lỗi hệ thống bất ngờ)
```json
{
  "success": false,
  "message": "Đã xảy ra lỗi hệ thống nghiêm trọng. Vui lòng liên hệ Admin.",
  "errors": [
    {
      "code": "INTERNAL_SERVER_ERROR",
      "message": "Object reference not set to an instance of an object."
    }
  ]
}
```

