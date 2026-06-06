using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:5173", "http://127.0.0.1:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddHttpClient("gateway-proxy")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false
    });

var app = builder.Build();

app.UseCors("Frontend");

var downstream = new DownstreamServices(
    ProductInventory: TrimTrailingSlash(builder.Configuration["DownstreamServices:ProductInventory"] ?? "http://localhost:5178"),
    OrderSales: TrimTrailingSlash(builder.Configuration["DownstreamServices:OrderSales"] ?? "http://localhost:5245"),
    UserReport: TrimTrailingSlash(builder.Configuration["DownstreamServices:UserReport"] ?? "http://localhost:5160"));

app.MapGet("/", () => Results.Ok(new
{
    service = "Retail API Gateway",
    routes = new
    {
        productInventory = downstream.ProductInventory,
        orderSales = downstream.OrderSales,
        userReport = downstream.UserReport
    }
}));

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "api-gateway" }));

app.MapMethods("/{**path}", ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"], async (
    HttpContext context,
    IHttpClientFactory httpClientFactory) =>
{
    var targetBaseUrl = ResolveTargetBaseUrl(context.Request.Path, downstream);
    if (targetBaseUrl == null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = $"Gateway route not configured for path '{context.Request.Path}'."
        });
        return;
    }

    var client = httpClientFactory.CreateClient("gateway-proxy");
    using var requestMessage = CreateProxyHttpRequest(context, targetBaseUrl);
    using var responseMessage = await client.SendAsync(
        requestMessage,
        HttpCompletionOption.ResponseHeadersRead,
        context.RequestAborted);

    context.Response.StatusCode = (int)responseMessage.StatusCode;
    CopyResponseHeaders(context, responseMessage);

    await responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
});

app.Run();

static string? ResolveTargetBaseUrl(PathString path, DownstreamServices downstream)
{
    if (path.StartsWithSegments("/vqr", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/orders", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/customers", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/customer-groups", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/suppliers", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/payments", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/promotions", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/shifts", StringComparison.OrdinalIgnoreCase))
    {
        return downstream.OrderSales;
    }

    if (path.StartsWithSegments("/api/products", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/categories", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/inventory", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/units", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/internal", StringComparison.OrdinalIgnoreCase))
    {
        return downstream.ProductInventory;
    }

    if (path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/users", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/reports", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/notifications", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/backups", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/hubs/notifications", StringComparison.OrdinalIgnoreCase))
    {
        return downstream.UserReport;
    }

    return null;
}

static HttpRequestMessage CreateProxyHttpRequest(HttpContext context, string targetBaseUrl)
{
    var targetUri = new Uri($"{targetBaseUrl}{context.Request.Path}{context.Request.QueryString}");
    var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);

    if (HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method))
    {
        requestMessage.Content = new StreamContent(context.Request.Body);
        if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
        {
            requestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }
    }

    foreach (var header in context.Request.Headers)
    {
        if (IsHopByHopHeader(header.Key) || string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
        {
            requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    return requestMessage;
}

static void CopyResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
{
    foreach (var header in responseMessage.Headers)
    {
        if (!IsHopByHopHeader(header.Key))
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
    }

    foreach (var header in responseMessage.Content.Headers)
    {
        if (!IsHopByHopHeader(header.Key))
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
    }

    context.Response.Headers.Remove("transfer-encoding");
}

static bool IsHopByHopHeader(string headerName)
{
    return string.Equals(headerName, "Connection", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(headerName, "Keep-Alive", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(headerName, "Proxy-Authenticate", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(headerName, "Proxy-Authorization", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(headerName, "TE", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(headerName, "Trailer", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(headerName, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(headerName, "Upgrade", StringComparison.OrdinalIgnoreCase);
}

static string TrimTrailingSlash(string value) => value.TrimEnd('/');

public record DownstreamServices(string ProductInventory, string OrderSales, string UserReport);
