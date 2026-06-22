using System.Text;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderSalesService.API.Middlewares;
using OrderSalesService.Application.Common.Behaviors;
using OrderSalesService.Application.Features.Orders.Commands.CreateOrder;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Mappings;
using OrderSalesService.API.Services;
using OrderSalesService.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderSalesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("OrderSalesService.Infrastructure")));

builder.Services.AddScoped<IOrderSalesDbContext>(provider =>
    provider.GetRequiredService<OrderSalesDbContext>());

builder.Services.AddHttpClient<IProductCostReader, ProductCostReader>(client =>
{
    var baseUrl = builder.Configuration["InternalApis:ProductService"] ?? "http://localhost:5178";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
});

// Register in-memory StockCache as a Singleton
builder.Services.AddSingleton<OrderSalesService.Application.Common.IStockCache, OrderSalesService.Application.Common.StockCache>();

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderSalesService.Infrastructure.Consumers.InventoryUpdatedConsumer>();
    x.AddConsumer<OrderSalesService.Infrastructure.Consumers.GoodsReceiptConfirmedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var hostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
        var userName = builder.Configuration["RabbitMQ:UserName"] ?? "guest";
        var password = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(hostName, "/", h =>
        {
            h.Username(userName);
            h.Password(password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var appAssembly = typeof(CreateOrderCommand).Assembly;
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(appAssembly));
builder.Services.AddValidatorsFromAssembly(appAssembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddAutoMapper(_ => { }, typeof(OrderSalesMappingProfile).Assembly);

var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? "ThisIsAVerySecretKeyThatMustBeAtLeast32CharactersLongToWorkWithHmacSha256!";
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "retail_user_report_service",
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "retail_app",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order & Sales Service API", Version = "v1",
        Description = "API quản lý đơn hàng, khách hàng, nhà cung cấp, thanh toán" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "Bearer",
        BearerFormat = "JWT", In = ParameterLocation.Header, Description = "Nhập JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

builder.Services.AddCors(opt =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:8080", "http://127.0.0.1:8080" };

    opt.AddPolicy("AllowAll", p => p.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<OrderSalesDbContext>();
        context.Database.Migrate();
        DbInitializer.SeedData(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order & Sales API v1")); }
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
