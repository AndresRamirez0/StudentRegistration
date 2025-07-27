using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configurar para Docker
builder.WebHost.UseUrls("http://0.0.0.0:80");

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

// Database
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IProfessorService, ProfessorService>();

// CORS - Permisivo para desarrollo
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

// Configuración de base de datos con reintentos
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // Esperar a que MySQL esté listo (importante en Docker)
    var retryCount = 0;
    var maxRetries = 30;
    
    while (retryCount < maxRetries)
    {
        try
        {
            logger.LogInformation("Intentando conectar a la base de datos... Intento {RetryCount}/{MaxRetries}", retryCount + 1, maxRetries);
            context.Database.EnsureCreated();
            
            if (!context.Professors.Any())
            {
                context.SaveChanges();
            }
            
            logger.LogInformation("✅ Base de datos configurada correctamente");
            break;
        }
        catch (Exception ex)
        {
            retryCount++;
            logger.LogWarning("❌ Error conectando a base de datos (intento {RetryCount}/{MaxRetries}): {Message}", retryCount, maxRetries, ex.Message);
            
            if (retryCount >= maxRetries)
            {
                logger.LogError("❌ No se pudo conectar a la base de datos después de {MaxRetries} intentos", maxRetries);
                throw;
            }
            
            await Task.Delay(2000); // Esperar 2 segundos antes del siguiente intento
        }
    }
}

// Pipeline HTTP
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Registration API v1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

// Endpoint de información
app.MapGet("/info", () => Results.Ok(new {
    api = "Student Registration API",
    version = "1.0.0",
    endpoints = new {
        swagger = "/swagger",
        health = "/health",
        students = "/api/students",
        courses = "/api/courses",
        professors = "/api/professors"
    }
}));

app.Run();

public partial class Program { }
