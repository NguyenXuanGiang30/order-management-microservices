using FluentValidation;
using System.Net;
using System.Text.Json;

namespace ProductInventoryService.API.Middlewares;

/// <summary>
/// Middleware bắt tất cả exception chưa xử lý, chuẩn hóa response lỗi dạng JSON.
/// Tuân theo RFC 7807 Problem Details.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for request {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToList();
            var response = new
            {
                Success = false,
                Message = "Dữ liệu không hợp lệ.",
                Errors = errors
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";

            var response = new { Success = false, Message = ex.Message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new { Success = false, Message = "Không có quyền truy cập." };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for request {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                Success = false,
                Message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau."
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
