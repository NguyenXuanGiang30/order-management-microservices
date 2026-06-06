# Bộ Tài liệu Thiết kế & Triển khai Kỹ thuật (Software Design Documentation)

Chuyên mục tài liệu thiết kế hệ thống chi tiết cho **Đề tài 01: Hệ thống Quản lý Bán hàng & Kho hàng (Kiến trúc Microservices)**. 

> [!IMPORTANT]
> 🎓 **[TÀI LIỆU BÁO CÁO MÔN HỌC KIẾN TRÚC & THIẾT KẾ PHẦN MỀM](./project-report.md)**
> Bản báo cáo học thuật đầy đủ 3 chương chi tiết, tập trung phân tích sâu vào **Order & Sales Service** cùng các mô hình biểu đồ Use Case, Activity, Sequence, Class, C4 Model (Level 1-4) và ERD hoàn chỉnh để bạn nộp bài tập lớn.

---

Bộ tài liệu này được xây dựng theo chuẩn công nghiệp nhằm cung cấp cho 3 nhóm phát triển độc lập một cái nhìn rõ ràng, chính xác về cấu trúc cơ sở dữ liệu, hợp đồng API, thiết kế sự kiện bất đồng bộ, quy trình tích hợp và kiểm thử phần mềm, giúp giảm thiểu tối đa xung đột khi phối hợp tích hợp hệ thống.

---

## 🏛️ Mục lục Tài liệu Thiết kế Hệ thống

Vui lòng tham khảo các tài liệu kỹ thuật chi tiết theo thứ tự khuyến nghị dưới đây:

### Phần I: Kiến trúc và Thiết kế cốt lõi
1. 🏛️ **[01. Kiến trúc Tổng quan (Architecture Design)](./01-architecture-design.md)**
   - Khái quát hệ thống, phân quyền người dùng, sơ đồ kiến trúc microservices toàn cảnh.
   - Nguyên tắc thiết kế, cấu trúc project thống nhất, luồng xử lý bán hàng, hủy đơn, trả hàng.
2. 🗄️ **[02. Thiết kế Cơ sở Dữ liệu (Database Design)](./02-database-design.md)**
   - Sơ đồ thực thể ERD Mermaid cụ thể cho 3 database: `ProductInventoryDB`, `OrderSalesDB`, `UserReportDB`.
   - Chiến lược thiết kế idempotency (`ProcessedEvents`), đánh Index tối ưu và chạy DB Migrations.
3. 🔌 **[03. Hợp đồng API (API Contracts)](./03-api-contracts.md)**
   - Đặc tả chi tiết các RESTful API của 3 Service qua Ocelot Gateway (Request/Response mẫu).
   - Thiết kế các API quản lý nhóm khách hàng, đổi mật khẩu, xem lịch sử đơn hàng và chuẩn hóa Error Responses.
4. ✉️ **[04. Kiến trúc Hướng Sự kiện (Event-Driven Design)](./04-event-driven-design.md)**
   - Sơ đồ Topic Exchange RabbitMQ, bảng định tuyến và chi tiết Payload các sự kiện (`stock.updated`, `order.created`, `order.cancelled`, `order.returned`, `payment.received`).
   - Kỹ thuật chống trùng lặp sự kiện (Idempotency) và Tắt máy an toàn (Graceful Shutdown).

### Phần II: Cấu hình và Hướng dẫn Triển khai
5. 🚀 **[05. Hướng dẫn Triển khai (Deployment Guide)](./05-deployment-guide.md)**
   - Kiến trúc container tối ưu RAM (chạy 1 instance SQL Server chứa 3 database).
   - Nội dung file cấu hình biến môi trường `.env`, Dockerfile templates đa tầng cho .NET 10 và Nginx VueJS.
   - Hướng dẫn vận hành Docker Compose và quy trình gỡ lỗi (Troubleshooting).
6. 🚪 **[06. Cấu hình Ocelot API Gateway (API Gateway Config)](./06-api-gateway-config.md)**
   - Chi tiết tệp cấu hình `ocelot.json` định tuyến cho cả 3 microservice.
   - Cơ chế bảo mật Jwt Validation chéo, giới hạn Rate Limiting và cấu hình chính sách CORS.

### Phần III: Quy trình và Phát triển Giao diện
7. 🖥️ **[07. Thiết kế Giao diện Frontend (Frontend Design)](./07-frontend-design.md)**
   - Sitemap giao diện theo phân quyền, cấu trúc thư mục dự án VueJS 3 + Vuetify 3.
   - Cấu hình điều hướng Vue Router, Route Guards bảo mật, Pinia Stores và cơ chế API Axios Interceptors.
8. 🚨 **[08. Xử lý Lỗi & Ghi nhật ký (Error Handling & Logging)](./08-error-handling.md)**
   - Quy chuẩn định dạng lỗi JSON thống nhất toàn hệ thống.
   - Cấu trúc Global Exception Middleware, cấu hình ghi log JSON với Serilog và phân phối Correlation ID bám vết request.
9. 🧪 **[09. Chiến lược Kiểm thử (Testing Strategy)](./09-testing-strategy.md)**
   - Quy chuẩn Unit Testing (xUnit, Moq, FluentAssertions) kèm mã mẫu.
   - Chiến lược Integration Testing dọn dẹp DB với Respawn, kiểm thử RabbitMQ và tự động hóa với Postman CLI (Newman).
10. 🔄 **[10. Quy trình Phát triển (Development Workflow)](./10-development-workflow.md)**
    - Mô hình GitFlow monorepo phối hợp cho 3 nhóm, quy tắc đặt tên branch, tiêu chuẩn viết code C# và Seed Data thử nghiệm mặc định.

---

## 📂 Tài liệu Tham khảo (References)
- 📄 **[Tài liệu Yêu cầu Đề tài Gốc](./references/00-requirements-original.md)**: Lưu giữ bản đặc tả yêu cầu nguyên bản của giảng viên để đối chiếu và đối soát.

---

## 👥 Phân chia Phụ trách phát triển giữa 3 Nhóm Sinh viên

| Nhóm | Component phụ trách | Phạm vi tài liệu cần nắm vững |
| :--- | :--- | :--- |
| **Nhóm 1** | **Product & Inventory Service** | `01-architecture`, `02-database` (ProductInventoryDB), `03-api-contracts` (Section 2), `04-event-driven` (stock.updated), `10-development-workflow` |
| **Nhóm 2** | **Order & Sales Service** | `01-architecture`, `02-database` (OrderSalesDB), `03-api-contracts` (Section 3), `04-event-driven` (order.*, payment.received), `10-development-workflow` |
| **Nhóm 3** | **User & Report Service** | `01-architecture`, `02-database` (UserReportDB), `03-api-contracts` (Section 1), `04-event-driven` (Consumers), `06-api-gateway`, `07-frontend-design` |
