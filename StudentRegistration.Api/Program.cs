using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Services;
using StudentRegistration.Api.Models.Entities; // ✅ AGREGAR ESTA LÍNEA
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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
        // ✅ CONFIGURACIÓN UTF-8 PARA JSON
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

// ✅ CONFIGURACIÓN UTF-8 PARA RESPUESTAS HTTP
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
        Description = "API para el sistema de registro de estudiantes con autenticación - Railway"
    });
    
    // ✅ CONFIGURAR SWAGGER PARA JWT
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer scheme. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Base de datos usando las variables de Railway MySQL
var connectionString = Environment.GetEnvironmentVariable("MYSQL_URL") 
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? Environment.GetEnvironmentVariable("MYSQL_PUBLIC_URL")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=student_db.sqlite";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (connectionString.StartsWith("mysql://"))
    {
        // Convertir URL de Railway MySQL
        var uri = new Uri(connectionString);
        var mysqlConnection = $"Server={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Uid={uri.UserInfo.Split(':')[0]};Pwd={uri.UserInfo.Split(':')[1]};SslMode=Required;Charset=utf8mb4;";
        // ✅ CONFIGURACIÓN UTF-8 SIMPLIFICADA (SIN CharSetBehavior)
        options.UseMySql(mysqlConnection, ServerVersion.AutoDetect(mysqlConnection));
    }
    else if (connectionString.Contains("Server=") || connectionString.Contains("server="))
    {
        // Cadena MySQL directa con charset UTF-8
        var updatedConnectionString = connectionString.Contains("Charset=") ? connectionString : connectionString + ";Charset=utf8mb4;";
        options.UseMySql(updatedConnectionString, ServerVersion.AutoDetect(updatedConnectionString));
    }
    else
    {
        // Fallback a SQLite
        options.UseSqlite(connectionString);
    }
});

// ✅ CONFIGURACIÓN JWT
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "StudentRegistrationSecretKey123456789";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "StudentRegistrationAPI";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "StudentRegistrationClient";

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

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

// Obtener logger
var appLogger = app.Services.GetRequiredService<ILogger<Program>>();

// Configuración de base de datos
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    appLogger.LogInformation("🔄 Configurando base de datos Railway MySQL...");
    appLogger.LogInformation("🔗 Connection String Type: {Type}", 
        connectionString.StartsWith("mysql://") ? "Railway MySQL URL" : 
        connectionString.Contains("Server=") ? "MySQL Direct" : "SQLite");
    
    // ✅ FORZAR RECREACIÓN PARA AGREGAR TABLA USERS
    appLogger.LogInformation("🗑️ Eliminando base de datos existente...");
    await context.Database.EnsureDeletedAsync();
    
    appLogger.LogInformation("🆕 Creando nueva estructura de base de datos...");
    await context.Database.EnsureCreatedAsync();
    
    appLogger.LogInformation("🌱 Creando datos semilla...");
    await context.SaveChangesAsync();
    
    // ✅ CREAR USUARIO ADMIN DINÁMICAMENTE SI NO EXISTE
    if (!await context.Users.AnyAsync(u => u.Username == "admin"))
    {
        appLogger.LogInformation("👤 Creando usuario administrador...");
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@university.edu",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            FirstName = "Sistema",
            LastName = "Administrador",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
        appLogger.LogInformation("✅ Usuario administrador creado: admin/admin123");
    }
    
    appLogger.LogInformation("✅ Base de datos Railway MySQL configurada con UTF-8");
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

// ✅ AGREGAR MIDDLEWARE DE AUTENTICACIÓN
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check mejorado
app.MapGet("/health", async (ApplicationDbContext context) => 
{
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        var connectionType = connectionString.StartsWith("mysql://") ? "Railway MySQL URL" : 
                           connectionString.Contains("Server=") ? "MySQL Direct" : "SQLite";
        
        return Results.Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            platform = "Railway.app",
            port = port,
            database = canConnect ? "connected" : "disconnected",
            connectionType = connectionType,
            encoding = "UTF-8"
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

// Debug endpoint actualizado
app.MapGet("/debug", () => Results.Ok(new {
    mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL") ?? "Not set",
    mysqlPublicUrl = Environment.GetEnvironmentVariable("MYSQL_PUBLIC_URL") ?? "Not set",
    databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ?? "Not set",
    mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST") ?? "Not set",
    mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "Not set",
    mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER") ?? "Not set",
    mysqlDatabase = Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "Not set",
    defaultConnection = builder.Configuration.GetConnectionString("Default") ?? "Not set",
    finalConnection = connectionString.Length > 50 ? connectionString.Substring(0, 50) + "..." : connectionString,
    encoding = Encoding.Default.EncodingName,
    consoleEncoding = Console.OutputEncoding.EncodingName
}));

app.MapGet("/info", () => Results.Ok(new {
    api = "Student Registration API",
    version = "1.0.0",
    platform = "Railway.app",
    encoding = "UTF-8",
    endpoints = new {
        swagger = "/",
        health = "/health",
        debug = "/debug",
        authStatus = "/test/auth-status",
        students = "/api/students",
        courses = "/api/courses",
        professors = "/api/professors",
        auth = "/api/auth"
    }
}));

// ✅ ENDPOINT TEMPORAL PARA VERIFICAR ESTADO DE AUTENTICACIÓN
app.MapGet("/test/auth-status", async (ApplicationDbContext context) => 
{
    try
    {
        var usersCount = await context.Users.CountAsync();
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        
        return Results.Ok(new { 
            totalUsers = usersCount,
            adminExists = adminUser != null,
            adminEmail = adminUser?.Email ?? "N/A",
            adminRole = adminUser?.Role ?? "N/A",
            adminCreatedAt = adminUser?.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
            databaseTables = new {
                users = await context.Users.CountAsync(),
                students = await context.Students.CountAsync(),
                professors = await context.Professors.CountAsync(),
                courses = await context.Courses.CountAsync(),
                studentCourses = await context.StudentCourses.CountAsync()
            },
            timestamp = DateTime.UtcNow,
            encoding = "UTF-8"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { 
            error = ex.Message,
            stackTrace = ex.StackTrace,
            timestamp = DateTime.UtcNow
        });
    }
});

// ✅ ENDPOINT PARA PROBAR CARACTERES UTF-8
app.MapGet("/test/utf8", () => Results.Ok(new {
    message = "Prueba de caracteres UTF-8",
    caracteres = new {
        acentos = "áéíóúñü",
        mayusculas = "ÁÉÍÓÚÑÜ",
        simbolos = "¡¿©®™€£¥",
        emojis = "🚀✅❌🔧📋",
        nombres = new[] { "José", "María", "Ángel", "Niño" },
        materias = new[] { "Matemáticas", "Física", "Química", "Programación" }
    },
    encoding = "UTF-8",
    timestamp = DateTime.UtcNow
}));

appLogger.LogInformation("🚀 Iniciando Student Registration API con UTF-8 en Railway - Puerto: {Port}", port);

app.Run();

public partial class Program { }