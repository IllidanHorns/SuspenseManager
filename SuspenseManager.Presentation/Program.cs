using System.Text;
using Application.Interfaces;
using Application.Services;
using Application.Validators;
using Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SuspenseManager.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// DbContext
builder.Services.AddDbContext<SuspenseManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application services
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IExcelParsingService, ExcelParsingService>();
builder.Services.AddScoped<ISuspenseService, SuspenseService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ITerritoryService, TerritoryService>();
builder.Services.AddScoped<IGroupProcessingService, GroupProcessingService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IMeService, MeService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IGroupingService, GroupingService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IBackOfficeService, BackOfficeService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IRightsCatalogService, RightsCatalogService>();
builder.Services.AddScoped<IAdminMetricsService, AdminMetricsService>();
builder.Services.AddScoped<IMonitorService, MonitorService>();
builder.Services.AddScoped<ISampleExcelService, SampleExcelService>();
builder.Services.AddHttpContextAccessor();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<SuspenseLineDtoValidator>();

// Controllers + JSON: игнорируем циклические ссылки
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey не настроен. Задайте переменную окружения Jwt__SecretKey.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SuspenseManager";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SuspenseManagerClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

// OpenAPI / Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Suspense Manager API",
        Version = "v1",
        Description = "API для управления суспенсами — невалидными записями из отчётов стриминговых платформ"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Вставьте JWT токен (без префикса Bearer — он добавится автоматически)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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

    // XML-комментарии для Swagger
    var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
    foreach (var xmlFile in xmlFiles)
    {
        c.IncludeXmlComments(xmlFile);
    }
});

var app = builder.Build();

// Авто-миграции + сидер при каждом старте (идемпотентно)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SuspenseManagerDbContext>();
    await db.Database.MigrateAsync();
    var adminPassword = app.Configuration["AdminPassword"] ?? "Admin123!";
    var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
    var operatorPasswordHash = BCrypt.Net.BCrypt.HashPassword("Operator123!");
    var backOfficePasswordHash = BCrypt.Net.BCrypt.HashPassword("Backoffice123!");
    await DatabaseSeeder.EnsurePasswordsAsync(db, adminPasswordHash);
    await DatabaseSeeder.SeedAsync(db, adminPasswordHash, operatorPasswordHash, backOfficePasswordHash);
}

// Глобальная обработка ошибок — первый middleware в пайплайне
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger доступен всегда (удобно для демонстрации)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Suspense Manager API v1");
    c.RoutePrefix = string.Empty;
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
