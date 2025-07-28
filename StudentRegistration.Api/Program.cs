using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Puerto dinámico para Render (usa puerto 10000 por defecto)
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
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
        Description = "API para el sistema de registro de estudiantes - Render Deploy"
    });
});

// Base de datos - Usar SQLite para simplicidad en Render free tier
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Para Render free tier, usar SQLite temporalmente
    var dbPath = Path.Combine(Environment.CurrentDirectory, "student_db.sqlite");
    options.UseSqlite($"Data Source={dbPath}");
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

// Configuración de base de datos automática
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    appLogger.LogInformation("🔄 Configurando base de datos SQLite...");
    
    // Crear base de datos
    await context.Database.EnsureCreatedAsync();
    
    // Crear datos semilla si no existen
    if (!await context.Professors.AnyAsync())
    {
        appLogger.LogInformation("🌱 Creando datos semilla...");
        await context.SaveChangesAsync();
    }
    
    appLogger.LogInformation("✅ Base de datos SQLite configurada correctamente");
}
catch (Exception ex)
{
    appLogger.LogError(ex, "❌ Error configurando base de datos");
}

// Habilitar Swagger en producción para Render
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Registration API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0",
    platform = "Render.com",
    port = port
}));

// Info endpoint
app.MapGet("/info", () => Results.Ok(new {
    api = "Student Registration API",
    version = "1.0.0",
    platform = "Render.com",
    endpoints = new {
        swagger = "/",
        health = "/health",
        students = "/api/students",
        courses = "/api/courses",
        professors = "/api/professors"
    }
}));

appLogger.LogInformation("🚀 Iniciando Student Registration API en Render - Puerto: {Port}", port);

app.Run();

public partial class Program { }