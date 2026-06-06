# 🏪 RetailOps — Order Management Microservices

Hệ thống **quản lý bán hàng và kho vận** xây dựng trên kiến trúc **Microservices**, sử dụng **.NET 8**, **Vue.js 3** (Vuetify), **RabbitMQ**, **SQL Server** và **Docker**.

> Đồ án Full-stack — Thiết kế hướng domain (DDD), giao tiếp bất đồng bộ qua message broker, API Gateway tập trung.

---

## 📐 Kiến trúc hệ thống

```
┌─────────────┐      ┌──────────────────┐
│  Vue.js 3   │─────▶│   API Gateway    │
│  (Vuetify)  │      │   (.NET 8)       │
└─────────────┘      └────────┬─────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
   │  Product &   │  │  Order &     │  │  User &      │
   │  Inventory   │  │  Sales       │  │  Report      │
   │  Service     │  │  Service     │  │  Service     │
   └──────┬───────┘  └──────┬───────┘  └──────┬───────┘
          │                 │                 │
          └────────┬────────┴────────┬────────┘
                   ▼                 ▼
            ┌────────────┐    ┌────────────┐
            │ SQL Server │    │  RabbitMQ  │
            └────────────┘    └────────────┘
```

## 🛠 Tech Stack

| Layer | Technology |
|-------|-----------|
| **Frontend** | Vue.js 3, TypeScript, Vuetify 3 (Materio Template) |
| **API Gateway** | ASP.NET Core 8, YARP Reverse Proxy |
| **Backend Services** | ASP.NET Core 8, Clean Architecture (DDD) |
| **Message Broker** | RabbitMQ 3 (MassTransit) |
| **Database** | SQL Server 2022, Entity Framework Core |
| **Containerization** | Docker, Docker Compose |
| **Authentication** | JWT Bearer Token |

## 📦 Microservices

### 1. Product & Inventory Service (Port 5001)
- Quản lý danh mục sản phẩm, đơn vị tính, nhà cung cấp
- Quản lý kho: nhập hàng, xuất hàng, kiểm kê tồn kho
- Cảnh báo hàng dưới ngưỡng an toàn

### 2. Order & Sales Service (Port 5002)
- Tạo và quản lý đơn hàng bán lẻ
- Quản lý khách hàng
- Tính chiết khấu, khuyến mãi
- Đồng bộ tồn kho qua RabbitMQ

### 3. User & Report Service (Port 5003)
- Đăng ký / Đăng nhập (JWT Authentication)
- Phân quyền người dùng (Role-based)
- Báo cáo doanh thu, lợi nhuận theo ngày/tháng
- Dashboard tổng hợp: Top sản phẩm, Top khách hàng
- Activity logs

### 4. API Gateway (Port 5000)
- Reverse proxy định tuyến request
- CORS configuration
- Centralized routing

## 🚀 Khởi chạy

### Yêu cầu
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Node.js 18+](https://nodejs.org/) (cho frontend development)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (cho backend development)

### Chạy toàn bộ hệ thống với Docker

```bash
# Clone repository
git clone https://github.com/NguyenXuanGiang30/order-management-microservices.git
cd order-management-microservices

# Khởi chạy tất cả services
docker-compose up -d
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:8080 |
| API Gateway | http://localhost:5000 |
| Product Service | http://localhost:5001 |
| Order Service | http://localhost:5002 |
| User/Report Service | http://localhost:5003 |
| RabbitMQ Management | http://localhost:15672 |

### Chạy Frontend riêng (Development)

```bash
cd frontend/typescript-version
npm install
npm run dev
```

## 📁 Cấu trúc thư mục

```
order-management-microservices/
├── src/                          # Backend services
│   ├── ApiGateway/               # API Gateway (YARP)
│   ├── OrderSalesService/        # Order & Sales microservice
│   │   ├── *.API/                # REST API layer
│   │   ├── *.Application/        # Business logic (CQRS, MediatR)
│   │   ├── *.Infrastructure/     # Data access (EF Core)
│   │   └── *.Tests/              # Unit tests
│   ├── ProductInventoryService/  # Product & Inventory microservice
│   ├── UserReportService/        # User & Report microservice
│   └── SharedContracts/          # Shared DTOs & events
├── frontend/
│   └── typescript-version/       # Vue.js 3 + Vuetify frontend
├── docs/                         # Architecture & API documentation
├── scripts/                      # DevOps scripts
├── docker-compose.yml            # Container orchestration
└── README.md
```

## 📊 Tính năng chính

- ✅ **Dashboard tổng quan** — Doanh thu hôm nay/tháng, biểu đồ 7 ngày, cảnh báo tồn kho
- ✅ **Quản lý sản phẩm** — CRUD sản phẩm, danh mục, đơn vị, nhà cung cấp
- ✅ **Quản lý kho** — Nhập hàng, xuất hàng, kiểm kê, cảnh báo ngưỡng
- ✅ **Quản lý đơn hàng** — Tạo đơn, thanh toán, lịch sử giao dịch
- ✅ **Báo cáo phân tích** — Lợi nhuận, top sản phẩm, top khách hàng, doanh số ngày
- ✅ **Phân quyền** — JWT Authentication, Role-based Authorization
- ✅ **Event-Driven** — Đồng bộ dữ liệu giữa services qua RabbitMQ

## 📚 Tài liệu

Xem thêm chi tiết trong thư mục [`docs/`](./docs/):
- [Thiết kế kiến trúc](./docs/01-architecture-design.md)
- [Thiết kế database](./docs/02-database-design.md)
- [API Contracts](./docs/03-api-contracts.md)
- [Event-Driven Design](./docs/04-event-driven-design.md)
- [Deployment Guide](./docs/05-deployment-guide.md)

## 👤 Tác giả

**Nguyễn Xuân Giang**
- GitHub: [@NguyenXuanGiang30](https://github.com/NguyenXuanGiang30)
