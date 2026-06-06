using FluentValidation;
using System.Net;
using System.Text.Json;

namespace UserReportService.API.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger) { _next = next; _logger = logger; }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            var errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToList();
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { Success = false, Message = "Dữ liệu không hợp lệ.", Errors = errors }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { Success = false, Message = "Đã xảy ra lỗi hệ thống." }));
        }
    }
}
