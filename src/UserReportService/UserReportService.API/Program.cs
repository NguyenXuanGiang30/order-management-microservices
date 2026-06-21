using System.Text;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
using UserReportService.API.Hubs;
using UserReportService.API.Middlewares;
using UserReportService.API.Security;
using UserReportService.API.Services;
using UserReportService.Application.Common.Security;
using UserReportService.Application.Features.Backups;
using UserReportService.Application.Features.Users.Commands.Login;
using UserReportService.Application.Interfaces;
using UserReportService.Application.Mappings;
using UserReportService.Infrastructure.Data;
using UserReportService.Infrastructure.Consumers;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<UserReportDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("UserReportService.Infrastructure")));

builder.Services.AddScoped<IUserReportDbContext>(provider =>
    provider.GetRequiredService<UserReportDbContext>());

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();
    x.AddConsumer<LowStockAlertConsumer>();
    x.AddConsumer<AuditLoggedConsumer>();

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

// JWT Token Service (Singleton vì chỉ phụ thuộc vào IConfiguration)
builder.Services.AddSingleton<JwtTokenService>();

// MediatR + AutoMapper
var appAssembly = typeof(LoginCommand).Assembly;
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(appAssembly));
builder.Services.AddAutoMapper(_ => { }, typeof(UserReportMappingProfile).Assembly);

// JWT Authentication
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
    opt.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrWhiteSpace(accessToken) &&
                context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<INotificationBroadcaster, SignalRNotificationBroadcaster>();
builder.Services.AddScoped<IUserReportBackupService, JsonUserReportBackupService>();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User & Report Service API", Version = "v1",
        Description = "API xác thực, quản lý người dùng, báo cáo doanh thu" });
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

    opt.AddPolicy("AllowAll", p => p.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

var app = builder.Build();

// Migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<UserReportDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "User & Report API v1")); }
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");
app.Run();
