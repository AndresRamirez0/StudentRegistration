using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Services;
using StudentRegistration.Api.Models.Entities;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text;

// ✅ CONFIGURACIÓN UTF-8 AL INICIO
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// Puerto dinámico para Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Student Registration API", 
        Version = "v1",
        Description = "API simple para el sistema de registro de estudiantes - Railway"
    });
    // ✅ SIN CONFIGURACIÓN JWT - MÁS SIMPLE
});

// Base de datos
var connectionString = Environment.GetEnvironmentVariable("MYSQL_URL") 
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? Environment.GetEnvironmentVariable("MYSQL_PUBLIC_URL")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=student_db.sqlite";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (connectionString.StartsWith("mysql://"))
    {
        var uri = new Uri(connectionString);
        var mysqlConnection = $"Server={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Uid={uri.UserInfo.Split(':')[0]};Pwd={uri.UserInfo.Split(':')[1]};SslMode=Required;Charset=utf8mb4;";
        options.UseMySql(mysqlConnection, ServerVersion.AutoDetect(mysqlConnection));
    }
    else if (connectionString.Contains("Server=") || connectionString.Contains("server="))
    {
        var updatedConnectionString = connectionString.Contains("Charset=") ? connectionString : connectionString + ";Charset=utf8mb4;";
        options.UseMySql(updatedConnectionString, ServerVersion.AutoDetect(updatedConnectionString));
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// ✅ SIN JWT - SISTEMA SIMPLE
// (Remover toda la configuración de Authentication/Authorization)

// AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IProfessorService, ProfessorService>();
builder.Services.AddScoped<IAuthService, AuthService>();

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

// Configuración de base de datos
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
    appLogger.LogInformation("🔄 Configurando base de datos...");
    
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();
    await context.SaveChangesAsync();
    
    if (!await context.Users.AnyAsync(u => u.Username == "admin"))
    {
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@university.edu",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), // ✅ CONTRASEÑA SUPER SIMPLE
            FirstName = "Sistema",
            LastName = "Administrador",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
        appLogger.LogInformation("✅ Usuario admin creado: admin/123"); // ✅ CONTRASEÑA SIMPLE
    }
}
catch (Exception ex)
{
    var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
    appLogger.LogError(ex, "❌ Error configurando base de datos");
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Registration API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");

// ✅ SIN AUTHENTICATION/AUTHORIZATION MIDDLEWARE

app.MapControllers();

// Endpoints de verificación
app.MapGet("/health", async (ApplicationDbContext context) => 
{
    var canConnect = await context.Database.CanConnectAsync();
    return Results.Ok(new { 
        status = "healthy", 
        timestamp = DateTime.UtcNow,
        database = canConnect ? "connected" : "disconnected",
        authType = "Simple (No JWT)"
    });
});

app.MapGet("/test/auth-status", async (ApplicationDbContext context) => 
{
    var usersCount = await context.Users.CountAsync();
    var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
    
    return Results.Ok(new { 
        totalUsers = usersCount,
        adminExists = adminUser != null,
        authType = "Simple Login (No JWT)",
        timestamp = DateTime.UtcNow
    });
});

app.Run();

public partial class Program { }