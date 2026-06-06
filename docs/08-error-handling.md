# Xử lý Lỗi & Ghi nhận Nhật ký (Error Handling & Logging)

Tài liệu này quy định kiến trúc xử lý lỗi tập trung, cơ chế logging có cấu trúc và giám sát sức khỏe (Health Checks) trên toàn bộ hệ thống Microservices.

---

## 1. Định dạng Phản hồi Lỗi Thống nhất (Standard Error Response)

Mọi API endpoint trong hệ thống khi gặp lỗi đều phải trả về mã trạng thái HTTP phù hợp kèm cấu trúc JSON nhất quán. Điều này giúp Frontend dễ dàng bắt lỗi và hiển thị thông báo trực quan cho người dùng.

```json
{
  "success": false,
  "message": "Thông điệp lỗi mô tả tổng quan sự cố bằng tiếng Việt hiển thị cho user",
  "errors": [
    {
      "field": "Tên_Trường_Gây_Lỗi (hoặc null nếu lỗi global)",
      "code": "MÃ_LỖI_HỆ_THỐNG (để frontend kiểm tra logic)",
      "message": "Mô tả chi tiết lỗi kỹ thuật bằng tiếng Anh hoặc tiếng Việt"
    }
  ]
}
```

### 1.1 HTTP Status Code Conventions
- `400 Bad Request`: Sử dụng cho lỗi validate dữ liệu đầu vào.
- `401 Unauthorized`: Token xác thực bị thiếu, không hợp lệ hoặc hết hạn.
- `402 Payment Required`: Dự phòng cho các giao dịch bị từ chối.
- `403 Forbidden`: Người dùng đã đăng nhập thành công nhưng không có quyền truy cập API yêu cầu.
- `404 Not Found`: Không tìm thấy tài nguyên (sản phẩm, đơn hàng, người dùng...).
- `409 Conflict`: Vi phạm ràng buộc logic nghiệp vụ (mã sản phẩm đã tồn tại, số lượng xuất vượt quá tồn kho...).
- `422 Unprocessable Entity`: Dữ liệu đúng định dạng nhưng vi phạm quy tắc logic nghiệp vụ phức tạp.
- `500 Internal Server Error`: Lỗi hệ thống nghiêm trọng chưa được xử lý (NullReferenceException, DB down...).

---

## 2. Middleware Xử lý Lỗi Tập trung (Global Exception Middleware)

Mỗi service .NET sử dụng một custom Middleware để tự động bắt tất cả các ngoại lệ (Exceptions) không được xử lý ở tầng controller, ghi log lỗi chi tiết và trả về cấu trúc lỗi chuẩn cho client:

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Một ngoại lệ chưa được xử lý đã xảy ra: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new StandardErrorResponse
        {
            Success = false,
            Message = "Đã xảy ra lỗi hệ thống. Vui lòng liên hệ quản trị viên."
        };

        switch (exception)
        {
            case BadHttpRequestException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Yêu cầu HTTP không hợp lệ.";
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Truy cập bị từ chối. Vui lòng đăng nhập lại.";
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "Tài nguyên yêu cầu không tồn tại.";
                break;

            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Message = exception.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                // Không nên tiết lộ chi tiết StackTrace cho Client ngoài môi trường Development
                response.Errors = new List<ErrorDetail>
                {
                    new ErrorDetail { Code = "INTERNAL_SERVER_ERROR", Message = exception.Message }
                };
                break;
        }

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        return context.Response.WriteAsync(json);
    }
}
```

---

## 3. Chiến lược Ghi nhận Nhật ký (Serilog Configuration)

Hệ thống sử dụng **Serilog** làm thư viện ghi log chính để hỗ trợ **Structured Logging** (Ghi log có cấu trúc dưới dạng JSON để phục vụ lưu trữ tập trung và tìm kiếm nhanh).

### Cấu hình `appsettings.json` cho Serilog:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
  }
}
```

---

## 4. Bám vết Giao dịch Phân tán (Correlation ID Strategy)

Khi một request đi từ Gateway đi qua nhiều microservices (ví dụ: tạo đơn hàng gọi kiểm tra giá chéo service), hệ thống sử dụng một mã **Correlation ID** duy nhất để bám vết luồng xử lý:

1. **API Gateway:** Kiểm tra xem header `X-Correlation-ID` có tồn tại trong request không. Nếu chưa có, tự sinh một mã GUID mới và đính kèm vào header.
2. **Microservices:** Đọc `X-Correlation-ID` từ header và ghi nhận mã này vào context của Serilog cho tất cả các bản ghi log liên quan.
3. **HTTP Client chéo service:** Khi một service gọi service khác, đính kèm `X-Correlation-ID` hiện tại vào header gọi đi.

```csharp
// Đăng ký Correlation ID Middleware trong mỗi Service
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    
    // Đính kèm vào Log Context để Serilog tự động ghi nhận vào mọi dòng log của request này
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});
```

---

## 5. Giám sát Sức khỏe Hệ thống (Health Checks)

Mỗi service triển khai các API `/health` (liveness check) và `/health/ready` (readiness check) để các hạ tầng như Docker Compose hay Kubernetes tự động theo dõi tình trạng hoạt động và restart khi bị lỗi treo.

```csharp
// Cấu hình trong Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), name: "sqlserver")
    .AddRabbitMQ(sp => sp.GetRequiredService<IConnection>(), name: "rabbitmq");

// Đăng ký Endpoint
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse // Trả về chi tiết JSON
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Chỉ kiểm tra xem API có đang chạy hay không
});
```
