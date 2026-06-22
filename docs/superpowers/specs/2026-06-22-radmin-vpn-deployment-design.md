# Tài liệu Thiết kế Triển khai Hệ thống qua Radmin VPN (Nhóm 2)

Tài liệu này đặc tả thiết kế cấu hình và triển khai hệ thống Microservices chia sẻ giữa 3 nhóm (Nhóm 1: Product, Nhóm 2: Order & Shared Infra, Nhóm 3: User Report) kết nối thông qua mạng Radmin VPN. Máy hiện tại là máy của **Nhóm 2**, chịu trách nhiệm điều phối và chạy các hạ tầng chung.

---

## 1. Phân bổ Dịch vụ & Mô hình mạng
*   **Mạng ảo:** Radmin VPN (Tất cả thành viên của 3 nhóm tham gia cùng một mạng).
*   **Địa chỉ IP giả định (để cấu hình mẫu):**
    *   `IP_NHOM_1` (Product): `26.11.11.11`
    *   `IP_NHOM_2` (Order & Shared Infra - Máy hiện tại): `26.22.22.22`
    *   `IP_NHOM_3` (User Report): `26.33.33.33`

### Phân bổ Containers chạy trên các máy:

| Máy / Nhóm | Dịch vụ Hạ tầng chạy | Dịch vụ Backend chạy | Cổng Public ra ngoài |
| :--- | :--- | :--- | :--- |
| **Nhóm 1 (Product)** | SQL Server (chứa ProductInventoryDB) | `product-inventory-service` | `5001` (API) |
| **Nhóm 2 (Order)** | SQL Server (OrderSalesDB), RabbitMQ | `order-sales-service`, `api-gateway`, `frontend` | `5000` (Gateway), `8080` (Frontend), `5672` (RabbitMQ) |
| **Nhóm 3 (User)** | SQL Server (UserReportDB) | `user-report-service` | `5003` (API) |

---

## 2. Giải pháp kỹ thuật & Các tệp cần cấu hình/tạo mới

### 2.1. Cập nhật `frontend/typescript-version/Dockerfile` [MODIFY]
*   **Vấn đề:** VueJS build tĩnh (static assets) tại thời điểm build Docker image. Nếu không truyền `VITE_API_BASE_URL`, nó sẽ mặc định gọi đến `localhost:5000`, khiến các máy Nhóm 1 và Nhóm 3 khi truy cập web của Nhóm 2 không thể gửi request đến API Gateway (vì trình duyệt của họ sẽ cố gửi đến `localhost:5000` của chính họ).
*   **Giải pháp:** Thêm `ARG VITE_API_BASE_URL` và `ENV VITE_API_BASE_URL` vào Dockerfile của frontend để truyền động IP Gateway của Nhóm 2 trong lúc build.

```dockerfile
FROM node:20-alpine AS build-stage
WORKDIR /app
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
COPY . .
RUN npm install
RUN npm run build
```

### 2.2. Tạo file cấu hình môi trường `.env.group2` [NEW]
*   Tạo file này ở thư mục gốc của dự án để Nhóm 2 cấu hình các IP Radmin VPN động của 3 nhóm và các thông tin mật khẩu DB, JWT.
*   Nội dung chứa các biến:
    *   `RADMIN_IP_NHOM1` (Mặc định: `26.11.11.11`)
    *   `RADMIN_IP_NHOM2` (Mặc định: `26.22.22.22`)
    *   `RADMIN_IP_NHOM3` (Mặc định: `26.33.33.33`)
    *   Các biến hạ tầng DB/JWT chung (lấy từ `.env.example`).

### 2.3. Tạo tệp `docker-compose.group2.yml` [NEW]
*   Đây là file Docker Compose tối ưu riêng cho máy Nhóm 2.
*   **Bao gồm các dịch vụ:**
    1.  `sqlserver`: Chỉ chạy và lưu DB cho Order Service (`OrderSalesDB`).
    2.  `rabbitmq`: Bật cổng `5672:5672` và `15672:15672` ra máy host để Nhóm 1 và 3 kết nối vào.
    3.  `order-sales-service`:
        *   Kết nối với local `sqlserver` và local `rabbitmq`.
        *   Cấu hình `InternalApis__ProductService=http://${RADMIN_IP_NHOM1}:5001` (Gọi chéo sang Nhóm 1).
    4.  `api-gateway`:
        *   Trỏ định tuyến downstream:
            *   Product: `http://${RADMIN_IP_NHOM1}:5001`
            *   Order (local): `http://order-sales-service:80`
            *   User Report: `http://${RADMIN_IP_NHOM3}:5003`
        *   Cấu hình CORS để cho phép máy nhóm khác truy cập:
            *   `Cors__AllowedOrigins__4=http://${RADMIN_IP_NHOM2}:8080`
    5.  `frontend`:
        *   Build với tham số `--build-arg VITE_API_BASE_URL=http://${RADMIN_IP_NHOM2}:5000`.

### 2.4. Tạo tài liệu hướng dẫn chi tiết `docs/11-multi-group-deployment-guide.md` [NEW]
*   Tài liệu Markdown chi tiết bằng tiếng Việt để hướng dẫn các nhóm (nhóm 1, 2, 3) cách cài đặt Radmin VPN, cấu hình IP và khởi chạy ứng dụng của nhóm mình thành công.

---

## 3. Kế hoạch Kiểm thử & Xác minh (Verification Plan)
1.  **Kiểm tra cú pháp Docker Compose:** Chạy `docker compose -f docker-compose.group2.yml config` để kiểm tra tính hợp lệ của file cấu hình.
2.  **Kiểm tra Build Frontend:** Xác thực build frontend nhận đúng biến môi trường thông qua docker build log.
3.  **Xác minh cấu hình Gateway:** Xác thực Ocelot Gateway của Nhóm 2 trỏ đúng các downstream service.
