using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Puerto dinámico para Railway (usa puerto 8080 por defecto)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Student Registration API", 
        Version = "v1",
        Description = "API para el sistema de registro de estudiantes - Railway Deploy"
    });
});

// Base de datos - MySQL para Railway con fallback a SQLite
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=student_db.sqlite";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (connectionString.StartsWith("mysql://") || connectionString.Contains("mysql"))
    {
        // Railway MySQL
        if (connectionString.StartsWith("mysql://"))
        {
            var uri = new Uri(connectionString);
            var mysqlConnection = $"Server={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Uid={uri.UserInfo.Split(':')[0]};Pwd={uri.UserInfo.Split(':')[1]};SslMode=Required;";
            options.UseMySql(mysqlConnection, ServerVersion.AutoDetect(mysqlConnection));
        }
        else
        {
            // Cadena MySQL directa
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }
    else
    {
        // Fallback a SQLite para desarrollo local
        options.UseSqlite(connectionString);
    }
});

// AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IProfessorService, ProfessorService>();

// CORS para Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Obtener logger una sola vez
var appLogger = app.Services.GetRequiredService<ILogger<Program>>();

// Configuración de base de datos con reintentos para Railway
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    appLogger.LogInformation("🔄 Configurando base de datos para Railway...");
    
    var retryCount = 0;
    var maxRetries = 15; // Más reintentos para Railway
    
    while (retryCount < maxRetries)
    {
        try
        {
            appLogger.LogInformation("📡 Intento {RetryCount}/{MaxRetries} conectando a base de datos", retryCount + 1, maxRetries);
            
            await context.Database.EnsureCreatedAsync();
            
            // Crear datos semilla si no existen
            if (!await context.Professors.AnyAsync())
            {
                appLogger.LogInformation("🌱 Creando datos semilla...");
                await context.SaveChangesAsync();
            }
            
            appLogger.LogInformation("✅ Base de datos configurada correctamente");
            break;
        }
        catch (Exception ex)
        {
            retryCount++;
            appLogger.LogWarning("⚠️ Error conectando a BD (intento {RetryCount}/{MaxRetries}): {Message}", 
                retryCount, maxRetries, ex.Message);
            
            if (retryCount >= maxRetries)
            {
                appLogger.LogError("❌ No se pudo conectar a la base de datos. Continuando sin BD...");
                break;
            }
            
            await Task.Delay(2000); // Esperar 2 segundos
        }
    }
}
catch (Exception ex)
{
    appLogger.LogError(ex, "❌ Error crítico en configuración de BD");
}

// Habilitar Swagger en Railway
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Registration API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Health check para Railway
app.MapGet("/health", async (ApplicationDbContext context) => 
{
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        return Results.Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            platform = "Railway.app",
            port = port,
            database = canConnect ? "connected" : "disconnected"
        });
    }
    catch
    {
        return Results.Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            platform = "Railway.app",
            port = port,
            database = "error"
        });
    }
});

// Info endpoint
app.MapGet("/info", () => Results.Ok(new {
    api = "Student Registration API",
    version = "1.0.0",
    platform = "Railway.app",
    endpoints = new {
        swagger = "/",
        health = "/health",
        students = "/api/students",
        courses = "/api/courses",
        professors = "/api/professors"
    }
}));

appLogger.LogInformation("🚀 Iniciando Student Registration API en Railway - Puerto: {Port}", port);

app.Run();

public partial class Program { }