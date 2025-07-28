using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Puerto dinámico para Railway
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
        Description = "API para el sistema de registro de estudiantes - Railway"
    });
});

// Base de datos simplificada para Railway
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? Environment.GetEnvironmentVariable("MYSQL_URL")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=student_db.sqlite";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (connectionString.StartsWith("mysql://"))
    {
        // Convertir URL de Railway MySQL
        var uri = new Uri(connectionString);
        var mysqlConnection = $"Server={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Uid={uri.UserInfo.Split(':')[0]};Pwd={uri.UserInfo.Split(':')[1]};SslMode=Required;";
        options.UseMySql(mysqlConnection, ServerVersion.AutoDetect(mysqlConnection));
    }
    else if (connectionString.Contains("Server=") || connectionString.Contains("server="))
    {
        // Cadena MySQL directa
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
    else
    {
        // Fallback a SQLite
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

// CORS
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

// Obtener logger
var appLogger = app.Services.GetRequiredService<ILogger<Program>>();

// Configuración de base de datos
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    appLogger.LogInformation("🔄 Configurando base de datos...");
    appLogger.LogInformation("🔗 Connection String Type: {Type}", 
        connectionString.StartsWith("mysql://") ? "MySQL URL" : 
        connectionString.Contains("Server=") ? "MySQL Direct" : "SQLite");
    
    await context.Database.EnsureCreatedAsync();
    
    if (!await context.Professors.AnyAsync())
    {
        appLogger.LogInformation("🌱 Creando datos semilla...");
        await context.SaveChangesAsync();
    }
    
    appLogger.LogInformation("✅ Base de datos configurada correctamente");
}
catch (Exception ex)
{
    appLogger.LogError(ex, "❌ Error configurando base de datos: {Message}", ex.Message);
}

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Registration API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Health check mejorado
app.MapGet("/health", async (ApplicationDbContext context) => 
{
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        var connectionType = connectionString.StartsWith("mysql://") ? "MySQL URL" : 
                           connectionString.Contains("Server=") ? "MySQL Direct" : "SQLite";
        
        return Results.Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            platform = "Railway.app",
            port = port,
            database = canConnect ? "connected" : "disconnected",
            connectionType = connectionType
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            platform = "Railway.app",
            port = port,
            database = "error",
            error = ex.Message
        });
    }
});

// Debug endpoint para verificar variables
app.MapGet("/debug", () => Results.Ok(new {
    databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ?? "Not set",
    mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL") ?? "Not set",
    defaultConnection = builder.Configuration.GetConnectionString("Default") ?? "Not set",
    finalConnection = connectionString.Length > 50 ? connectionString.Substring(0, 50) + "..." : connectionString
}));

app.MapGet("/info", () => Results.Ok(new {
    api = "Student Registration API",
    version = "1.0.0",
    platform = "Railway.app",
    endpoints = new {
        swagger = "/",
        health = "/health",
        debug = "/debug",
        students = "/api/students",
        courses = "/api/courses",
        professors = "/api/professors"
    }
}));

appLogger.LogInformation("🚀 Iniciando Student Registration API en Railway - Puerto: {Port}", port);

app.Run();

public partial class Program { }