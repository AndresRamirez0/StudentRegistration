using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configurar puerto dinámico para Railway
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
        Description = "API para el sistema de registro de estudiantes"
    });
});

// Database - Configuración flexible para Railway
var connectionString = builder.Configuration.GetConnectionString("Default") 
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? "Server=localhost;Database=student_db;Uid=root;Pwd=test;SslMode=None;";

// Convertir DATABASE_URL de Railway si es necesario
if (connectionString.StartsWith("mysql://"))
{
    var uri = new Uri(connectionString);
    connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Uid={uri.UserInfo.Split(':')[0]};Pwd={uri.UserInfo.Split(':')[1]};SslMode=Required;";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    try
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
    catch
    {
        // Fallback si no puede detectar la versión
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
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

// CORS - Más restrictivo para producción
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

// Obtener logger una sola vez al inicio
var appLogger = app.Services.GetRequiredService<ILogger<Program>>();

// Configuración de base de datos con manejo robusto de errores
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    appLogger.LogInformation("🚀 Iniciando configuración de base de datos...");
    
    // Intentar conectar con timeout más corto para Railway
    var retryCount = 0;
    var maxRetries = 10; // Reducido para Railway
    
    while (retryCount < maxRetries)
    {
        try
        {
            appLogger.LogInformation("📡 Intento {RetryCount}/{MaxRetries} conectando a base de datos", retryCount + 1, maxRetries);
            
            await context.Database.EnsureCreatedAsync();
            
            // Solo crear datos semilla si no existen
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
                // No lanzar excepción para que la app inicie sin BD
                break;
            }
            
            await Task.Delay(1000); // Delay más corto para Railway
        }
    }
}
catch (Exception ex)
{
    appLogger.LogError(ex, "❌ Error crítico en configuración de BD. La app continuará sin base de datos.");
}

// Pipeline HTTP
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("ENABLE_SWAGGER") == "true")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Registration API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Health check mejorado
app.MapGet("/health", async (ApplicationDbContext context) => 
{
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        return Results.Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            database = canConnect ? "connected" : "disconnected",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }
    catch
    {
        return Results.Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            database = "error",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }
});

// Endpoint de información
app.MapGet("/info", () => Results.Ok(new {
    api = "Student Registration API",
    version = "1.0.0",
    port = port,
    endpoints = new {
        swagger = "/swagger",
        health = "/health",
        students = "/api/students",
        courses = "/api/courses",
        professors = "/api/professors"
    }
}));

appLogger.LogInformation("🚀 Iniciando aplicación en puerto {Port}", port);

app.Run();

public partial class Program { }
