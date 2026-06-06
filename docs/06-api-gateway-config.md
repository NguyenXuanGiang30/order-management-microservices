# Cấu hình Ocelot API Gateway (Ocelot Config)

Tài liệu này hướng dẫn cách cấu hình và triển khai **Ocelot API Gateway** đóng vai trò là điểm đầu vào (Single Entry Point) duy nhất cho toàn bộ hệ thống Microservices.

---

## 1. Vai trò của API Gateway trong Hệ thống

API Gateway (`api-gateway`) chạy trên cổng `5000` và thực hiện các chức năng cốt lõi sau:
1. **Định tuyến (Routing):** Nhận request từ Frontend VueJS và chuyển tiếp (forward) đến đúng microservice tương ứng dựa trên URL path.
2. **Xác thực JWT (JWT Authentication Verification):** Tự động validate JWT Bearer token trong header `Authorization` trước khi chuyển tiếp request. Nếu token không hợp lệ hoặc hết hạn, Gateway sẽ trả về lỗi `401 Unauthorized` ngay lập tức mà không làm phiền đến các microservice phía sau.
3. **Giới hạn lượt gọi (Rate Limiting):** Chống tấn công DDoS và giảm tải cho hệ thống bằng cách giới hạn số lượng request tối đa từ một IP trong một khoảng thời gian nhất định.
4. **Cấu hình CORS (Cross-Origin Resource Sharing):** Cho phép Frontend VueJS gọi API từ các domain khác nhau.

---

## 2. File cấu hình `ocelot.json` chi tiết

Dưới đây là nội dung chi tiết của file cấu hình `ocelot.json` nằm trong dự án `ApiGateway`:

```json
{
  "Routes": [
    // =========================================================================
    // 1. ROUTING CHO USER & REPORT SERVICE (PORT 5003)
    // =========================================================================
    {
      "DownstreamPathTemplate": "/api/auth/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "user-report-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/auth/{everything}",
      "UpstreamHttpMethod": [ "Post", "Put", "Get" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      }
    },
    {
      "DownstreamPathTemplate": "/api/users/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "user-report-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/users/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      }
    },
    {
      "DownstreamPathTemplate": "/api/reports/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "user-report-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/reports/{everything}",
      "UpstreamHttpMethod": [ "Get" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      },
      "RateLimitOptions": {
        "ClientWhitelist": [],
        "EnableRateLimiting": true,
        "Period": "1m",
        "PeriodTimespan": 5,
        "Limit": 30
      }
    },

    // =========================================================================
    // 2. ROUTING CHO PRODUCT & INVENTORY SERVICE (PORT 5001)
    // =========================================================================
    {
      "DownstreamPathTemplate": "/api/products/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "product-inventory-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/products/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      }
    },
    {
      "DownstreamPathTemplate": "/api/categories/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "product-inventory-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/categories/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      }
    },
    {
      "DownstreamPathTemplate": "/api/units/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "product-inventory-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/units/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      }
    },
    {
      "DownstreamPathTemplate": "/api/inventory/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "product-inventory-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/inventory/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      }
    },

    // =========================================================================
    // 3. ROUTING CHO ORDER & SALES SERVICE (PORT 5002)
    // =========================================================================
    {
      "DownstreamPathTemplate": "/api/orders/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "order-sales-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/orders/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      }
    },
    {
      "DownstreamPathTemplate": "/api/customers/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "order-sales-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/customers/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      }
    },
    {
      "DownstreamPathTemplate": "/api/customer-groups/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "order-sales-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/customer-groups/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      }
    },
    {
      "DownstreamPathTemplate": "/api/suppliers/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "order-sales-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/suppliers/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      }
    },
    {
      "DownstreamPathTemplate": "/api/payments/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "order-sales-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/payments/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      }
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

---

## 3. Cơ chế Bảo mật và Xác thực JWT

1. **Shared Key Validation:**
   - Gateway sử dụng cùng một cấu hình **JwtSettings.Secret** với `UserReportService`. Khi nhận được request mang header `Authorization: Bearer <token>`, Gateway tự giải mã token và kiểm tra tính hợp lệ về mặt chữ ký (signature) và thời gian hết hạn (expiration).
2. **Cơ chế hoạt động:**
   ```mermaid
   sequenceDiagram
       participant FE as Frontend App
       participant GW as API Gateway (Ocelot)
       participant US as User & Report Service
       
       FE->>GW: Gửi request + JWT Bearer Token
       GW->>GW: Validate JWT locally (using shared secret)
       alt Token không hợp lệ / Hết hạn
           GW-->>FE: Trả về 401 Unauthorized
       else Token hợp lệ
           GW->>US: Forward Request
           US-->>GW: Trả về kết quả 200 OK
           GW-->>FE: Chuyển tiếp kết quả
       end
   ```

---

## 4. Giới hạn Tần suất gọi (Rate Limiting)

Để tránh hiện tượng càn quét hoặc spam các API nặng (như API Báo cáo & Dashboard), Gateway cấu hình giới hạn Rate Limiting trên route `/api/reports/*`:
- `EnableRateLimiting`: `true` - Kích hoạt giới hạn.
- `Period`: `1m` - Chu kỳ kiểm tra là 1 phút.
- `PeriodTimespan`: `5s` - Khi một IP vượt quá giới hạn, IP đó sẽ bị khóa tạm thời trong 5 giây.
- `Limit`: `30` - Tối đa 30 request mỗi phút từ một client IP.

Nếu vượt quá số lượng trên, Gateway sẽ tự động chặn và trả về mã lỗi HTTP `429 Too Many Requests`.

---

## 5. Cấu hình CORS trong API Gateway

Để Frontend chạy ở `http://localhost:8080` có thể gọi API ở cổng `5000` mà không bị lỗi CORS, trong file `Program.cs` của API Gateway phải thêm cấu hình sau:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:8080") // Chỉ cho phép VueJS gọi tới
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Thêm dịch vụ Ocelot và Authentication
builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"])),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddOcelot();

var app = builder.Build();

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
await app.UseOcelot();
app.Run();
```
