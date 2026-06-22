# Hướng dẫn Triển khai Liên nhóm qua Radmin VPN (Multi-Group Deployment Guide)

Tài liệu này hướng dẫn chi tiết cách cấu hình, triển khai và liên kết hệ thống microservices giữa **3 nhóm** làm việc từ xa bằng cách sử dụng mạng riêng ảo **Radmin VPN** và **Docker Compose**.

Mô hình này đã được tối ưu để **đạt mức cấu hình bằng không (Zero-Config) cho Nhóm 1 và Nhóm 3**. Họ chỉ cần clone code về, bật Radmin VPN và chạy một tệp script tự động.

---

## 1. Kiến trúc phân bổ hệ thống (System Topology)

Mỗi nhóm tự chạy Database của riêng mình để tự chủ dữ liệu phát triển. Các dịch vụ hạ tầng chung (Gateway, Frontend, RabbitMQ) sẽ được chạy tập trung trên máy của **Nhóm 2 (Order Service)**.

```mermaid
graph TD
    subgraph Machine_Nhom1 [Máy Nhóm 1 - Product Service]
        DB1[(SQL Server<br/>ProductInventoryDB)]
        ProductService[Product Service<br/>Cổng: 5001]
    end

    subgraph Machine_Nhom2 [Máy Nhóm 2 - Order & Shared Infra]
        DB2[(SQL Server<br/>OrderSalesDB)]
        OrderService[Order Service<br/>Cổng: 5002]
        Gateway[Ocelot API Gateway<br/>Cổng: 5000]
        RabbitMQ[RabbitMQ Message Broker<br/>Cổng: 5672]
        Frontend[Nginx VueJS Web App<br/>Cổng: 8080]
    end

    subgraph Machine_Nhom3 [Máy Nhóm 3 - User Service]
        DB3[(SQL Server<br/>UserReportDB)]
        UserService[User Service<br/>Cổng: 5003]
    end

    %% Truy cập Web
    Client[Browser của các thành viên] -->|http://IP_NHOM_2:8080| Frontend
    
    %% API call qua Radmin VPN
    Frontend -->|Gọi API| Gateway
    Gateway -->|Local call| OrderService
    Gateway -->|Qua VPN| ProductService
    Gateway -->|Qua VPN| UserService

    OrderService -->|HTTP Call qua VPN| ProductService
    
    %% Đồng bộ Event-Driven qua RabbitMQ chung của Nhóm 2
    ProductService -->|Kết nối RabbitMQ qua VPN| RabbitMQ
    UserService -->|Kết nối RabbitMQ qua VPN| RabbitMQ
    OrderService -->|Kết nối RabbitMQ local| RabbitMQ
```

---

## 2. Bước 1: Thiết lập mạng ảo Radmin VPN (Tất cả các máy)

Tất cả các thành viên trong các nhóm cần tải và cài đặt [Radmin VPN](https://www.radmin-vpn.com/).

1.  **Tạo mạng chung (Chỉ cần 1 người tạo):**
    *   Mở Radmin VPN -> Chọn **Network** -> **Create New Network**.
    *   Đặt tên mạng (ví dụ: `Retail-Microservices-BTL`) và mật khẩu.
2.  **Tham gia mạng (Thực hiện bởi các thành viên khác):**
    *   Chọn **Network** -> **Join an Existing Network**.
    *   Nhập tên mạng và mật khẩu vừa tạo.
3.  **Lấy địa chỉ IP Radmin VPN:**
    *   Ghi lại IP dạng `26.x.x.x` hiển thị bên cạnh tên máy trong Radmin VPN của 3 máy chạy Backend đại diện cho 3 nhóm.

---

## 3. Bước 2: Nhóm 2 cấu hình IP VPN chung của các nhóm

Vì tệp `.env.radmin` được lưu trên Git và chia sẻ chung, **chỉ có duy nhất máy Nhóm 2 (bạn)** cần chỉnh sửa tệp này để nhập đúng IP Radmin VPN thực tế của cả 3 nhóm.

1. Mở tệp `.env.radmin` ở thư mục gốc dự án.
2. Cập nhật các dòng IP tương ứng với IP Radmin VPN thực tế của các nhóm:
   ```ini
   RADMIN_IP_NHOM1=26.x.x.x  # Nhập IP của máy chạy Backend Nhóm 1
   RADMIN_IP_NHOM2=26.y.y.y  # Nhập IP của máy Nhóm 2 (máy của bạn)
   RADMIN_IP_NHOM3=26.z.z.z  # Nhập IP của máy chạy Backend Nhóm 3
   ```
3. Commit và Push tệp `.env.radmin` này lên Git:
   ```bash
   git add .env.radmin
   git commit -m "chore(env): update radmin vpn IPs for group deployment"
   git push
   ```

---

## 4. Bước 3: Cách vận hành của từng nhóm (Quy trình 1-Click)

Sau khi Nhóm 2 đã cập nhật và đẩy IP lên Git, các nhóm chỉ cần thực hiện như sau:

### 4.1. Đối với máy Nhóm 2 (Order & Shared Infra)
Double-click chuột vào tệp `run-nhom2.bat` (hoặc chạy lệnh: `docker compose --env-file .env.radmin -f docker-compose.group2.yml up -d --build`).

### 4.2. Đối với máy Nhóm 1 (Product & Inventory)
1. Thực hiện lệnh `git pull` để nhận tệp `.env.radmin` chứa IP mới nhất.
2. Bật Radmin VPN và đảm bảo ping thông tới máy Nhóm 2 (`ping 26.y.y.y`).
3. Double-click chuột vào tệp `run-nhom1.bat` để chạy Docker (hoặc chạy lệnh: `docker compose --env-file .env.radmin -f docker-compose.group1.yml up -d --build`).

### 4.3. Đối với máy Nhóm 3 (User & Report)
1. Thực hiện lệnh `git pull` để nhận tệp `.env.radmin` chứa IP mới nhất.
2. Bật Radmin VPN và đảm bảo ping thông tới máy Nhóm 2 (`ping 26.y.y.y`).
3. Double-click chuột vào tệp `run-nhom3.bat` để chạy Docker (hoặc chạy lệnh: `docker compose --env-file .env.radmin -f docker-compose.group3.yml up -d --build`).

---

## 5. Thứ tự khởi chạy khuyên dùng

Để tránh trường hợp các Backend service của Nhóm 1 và Nhóm 3 bị ngắt kết nối do không tìm thấy RabbitMQ chung (chạy ở Nhóm 2):

1. **Nhóm 2** bật Radmin VPN và chạy trước `run-nhom2.bat`.
2. Đợi cho đến khi container `retail_rabbitmq_nhom2` của Nhóm 2 có trạng thái **healthy** (khoảng 30 giây đến 1 phút).
3. **Nhóm 1 & 3** kéo code mới nhất về (`git pull`) rồi chạy tệp `run-nhom1.bat` / `run-nhom3.bat` tương ứng.

---

## 6. Khắc phục sự cố & Gỡ lỗi (Troubleshooting)

### 6.1. Lỗi Windows Defender Firewall chặn kết nối
Nếu các nhóm đã kết nối vào Radmin VPN và ping thành công nhưng Docker báo lỗi không kết nối được RabbitMQ hoặc API Gateway:
*   **Cách khắc phục (Thực hiện trên tất cả các máy):**
    1.  Mở **Windows Defender Firewall** -> Chọn **Advanced Settings**.
    2.  Chọn mục **Inbound Rules** -> Nhấp **New Rule...** ở khung bên phải.
    3.  Chọn loại rule là **Port** -> Chọn **TCP** và chỉ định các cổng cần mở: `1433, 5672, 15672, 5000, 5001, 5002, 5003, 8080`.
    4.  Chọn **Allow the connection** -> Tick chọn tất cả các mạng (Domain, Private, Public) -> Đặt tên rule là `Radmin VPN Allow Ports` và lưu lại.

### 6.2. Lỗi CORS (Cross-Origin Resource Sharing) tại Gateway
Nếu trình duyệt của thành viên Nhóm 1 hoặc Nhóm 3 truy cập được giao diện Frontend nhưng khi thao tác hoặc đăng nhập bị lỗi CORS đỏ lòm ở Console F12:
*   **Nguyên nhân:** API Gateway chặn các domain không được cấu hình trong CORS.
*   **Cách khắc phục:** Đảm bảo cấu hình `Cors__AllowedOrigins__4` trong `docker-compose.group2.yml` trùng khớp với `http://${RADMIN_IP_NHOM2}:8080`. Khi họ truy cập qua IP VPN của bạn, origin gửi lên sẽ là IP đó và được Gateway cho phép.
