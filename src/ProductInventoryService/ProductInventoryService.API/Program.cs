using System.Text;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProductInventoryService.API.Middlewares;
using ProductInventoryService.Application.Common.Behaviors;
using ProductInventoryService.Application.Features.Products.Commands.CreateProduct;
using ProductInventoryService.Application.Interfaces;
using ProductInventoryService.Application.Mappings;
using ProductInventoryService.Infrastructure.Data;
using ProductInventoryService.Infrastructure.Consumers;

var builder = WebApplication.CreateBuilder(args);

// DbContext configuration
builder.Services.AddDbContext<ProductInventoryDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("ProductInventoryService.Infrastructure")));

builder.Services.AddScoped<IProductInventoryDbContext>(provider =>
    provider.GetRequiredService<ProductInventoryDbContext>());

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();
    x.AddConsumer<OrderReturnedConsumer>();

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

// =============================================
// 2. MediatR + CQRS Pipeline + FluentValidation
// =============================================
var applicationAssembly = typeof(CreateProductCommand).Assembly;

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(applicationAssembly));

builder.Services.AddValidatorsFromAssembly(applicationAssembly);

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// =============================================
// 3. AutoMapper
// =============================================
builder.Services.AddAutoMapper(_ => { }, typeof(ProductInventoryMappingProfile).Assembly);

// =============================================
// 4. JWT Authentication
// =============================================
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? "ThisIsAVerySecretKeyThatMustBeAtLeast32CharactersLongToWorkWithHmacSha256!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "retail_user_report_service";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "retail_app";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

// =============================================
// 5. Controllers + Swagger
// =============================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product & Inventory Service API",
        Version = "v1",
        Description = "API quản lý sản phẩm, danh mục, tồn kho, phiếu nhập kho"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token. Ví dụ: eyJhbGciOi..."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// =============================================
// 6. CORS
// =============================================
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:8080", "http://127.0.0.1:8080" };

    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ProductInventoryDbContext>();
        context.Database.Migrate();
        ProductInventoryService.Infrastructure.Data.DbInitializer.SeedData(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// =============================================
// Middleware Pipeline
// =============================================
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product & Inventory API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
