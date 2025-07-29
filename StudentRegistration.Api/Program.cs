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
    c.SwaggerDoc("v1", new()
    {
        Title = "Student Registration API",
        Version = "v1",
        Description = "API simple para el sistema de registro de estudiantes - Railway"
    });
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

// ✅ CONFIGURACIÓN DE BASE DE DATOS MEJORADA CON DEBUGGING
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
    appLogger.LogInformation("🔄 Configurando base de datos...");

    // ✅ ASEGURAR QUE LA BASE DE DATOS SE CREE CORRECTAMENTE
    appLogger.LogInformation("🗑️ Eliminando base de datos existente...");
    await context.Database.EnsureDeletedAsync();

    appLogger.LogInformation("🆕 Creando nueva estructura de base de datos...");
    await context.Database.EnsureCreatedAsync();

    appLogger.LogInformation("🌱 Creando datos semilla...");
    await context.SaveChangesAsync();

    // ✅ FORZAR CREACIÓN DEL USUARIO ADMIN SIEMPRE
    appLogger.LogInformation("👤 Verificando usuario admin...");
    var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");

    if (existingAdmin == null)
    {
        appLogger.LogInformation("🔨 Creando usuario admin...");
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@university.edu",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
            FirstName = "Sistema",
            LastName = "Administrador",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
        appLogger.LogInformation("✅ Usuario admin creado exitosamente: admin/123");

        // ✅ VERIFICAR QUE SE GUARDÓ CORRECTAMENTE
        var verifyAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (verifyAdmin != null)
        {
            appLogger.LogInformation("✅ Verificación: Usuario admin encontrado con ID: {Id}", verifyAdmin.Id);
        }
        else
        {
            appLogger.LogError("❌ Error: Usuario admin no se pudo verificar después de crearlo");
        }
    }
    else
    {
        appLogger.LogInformation("✅ Usuario admin ya existe con ID: {Id}", existingAdmin.Id);
    }

    // ✅ MOSTRAR ESTADÍSTICAS DE LA BASE DE DATOS
    var userCount = await context.Users.CountAsync();
    var professorCount = await context.Professors.CountAsync();
    var courseCount = await context.Courses.CountAsync();

    appLogger.LogInformation("📊 Estadísticas de BD: Users={UserCount}, Professors={ProfessorCount}, Courses={CourseCount}",
        userCount, professorCount, courseCount);

    appLogger.LogInformation("✅ Base de datos configurada correctamente");
}
catch (Exception ex)
{
    var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
    appLogger.LogError(ex, "❌ Error configurando base de datos: {Message}", ex.Message);
    appLogger.LogError("❌ StackTrace: {StackTrace}", ex.StackTrace);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Registration API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");

app.MapControllers();

// ✅ ENDPOINTS DE DEBUGGING
app.MapGet("/health", async (ApplicationDbContext context) =>
{
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = canConnect ? "connected" : "disconnected",
            authType = "Simple (No JWT)"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            status = "error",
            message = ex.Message,
            timestamp = DateTime.UtcNow
        });
    }
});

app.MapGet("/test/auth-status", async (ApplicationDbContext context) =>
{
    try
    {
        var usersCount = await context.Users.CountAsync();
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        var allUsers = await context.Users.Select(u => new { u.Id, u.Username, u.Email, u.Role, u.IsActive }).ToListAsync();

        return Results.Ok(new
        {
            totalUsers = usersCount,
            adminExists = adminUser != null,
            adminDetails = adminUser != null ? new
            {
                id = adminUser.Id,
                username = adminUser.Username,
                email = adminUser.Email,
                role = adminUser.Role,
                isActive = adminUser.IsActive,
                hasPassword = !string.IsNullOrEmpty(adminUser.PasswordHash)
            } : null,
            allUsers = allUsers,
            authType = "Simple Login (No JWT)",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            error = ex.Message,
            timestamp = DateTime.UtcNow
        });
    }
});

// ✅ ENDPOINT PARA VERIFICAR ESTUDIANTES VS USUARIOS
app.MapGet("/test/students-vs-users", async (ApplicationDbContext context) =>
{
    try
    {
        var allUsers = await context.Users
            .Where(u => u.IsActive)
            .Select(u => new { u.Id, u.Username, u.Role, u.StudentId, u.Email })
            .ToListAsync();

        var allStudents = await context.Students
            .Select(s => new { s.Id, s.FirstName, s.LastName, s.Email, s.StudentCode })
            .ToListAsync();

        var usersWithStudentRole = allUsers.Where(u =>
            u.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) ||
            u.Role.Equals("student", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(u.Role)).ToList();

        var usersWithoutStudentRecord = usersWithStudentRole.Where(u => u.StudentId == null).ToList();

        return Results.Ok(new
        {
            totalUsers = allUsers.Count,
            totalStudents = allStudents.Count,
            usersWithStudentRole = usersWithStudentRole.Count,
            usersWithoutStudentRecord = usersWithoutStudentRecord.Count,
            problemUsers = usersWithoutStudentRecord,
            allStudents = allStudents,
            allUsers = allUsers
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ✅ ENDPOINT PARA MIGRAR USUARIOS EXISTENTES A ESTUDIANTES
app.MapPost("/admin/migrate-students", async (ApplicationDbContext context) =>
{
    try
    {
        // Buscar usuarios que deberían ser estudiantes pero no tienen registro Student
        var usersToMigrate = await context.Users
            .Where(u => u.IsActive &&
                       (u.Role.ToLower() == "student" || u.Role == "") &&
                       u.StudentId == null)
            .ToListAsync();

        var migratedCount = 0;

        foreach (var user in usersToMigrate)
        {
            // Crear registro Student
            var student = new Student
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                StudentCode = await GenerateStudentCodeAsync(context),
                RegistrationDate = user.CreatedAt,
                TotalCredits = 0
            };

            context.Students.Add(student);
            await context.SaveChangesAsync();

            // Actualizar usuario
            user.StudentId = student.Id;
            user.Role = "Student"; // Normalizar rol
            migratedCount++;
        }

        await context.SaveChangesAsync();

        return Results.Ok(new
        {
            message = $"Migración completada: {migratedCount} usuarios migrados a estudiantes",
            migratedCount = migratedCount,
            migratedUsers = usersToMigrate.Select(u => new { u.Id, u.Username, u.Email }).ToList()
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ✅ ENDPOINT PARA RECREAR USUARIO ADMIN
app.MapPost("/admin/recreate-admin", async (ApplicationDbContext context) =>
{
    try
    {
        // Eliminar admin existente
        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (existingAdmin != null)
        {
            context.Users.Remove(existingAdmin);
            await context.SaveChangesAsync();
        }

        // Crear nuevo admin
        var newAdmin = new User
        {
            Username = "admin",
            Email = "admin@university.edu",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
            FirstName = "Sistema",
            LastName = "Administrador",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Users.Add(newAdmin);
        await context.SaveChangesAsync();

        return Results.Ok(new
        {
            message = "Usuario admin recreado exitosamente",
            username = "admin",
            password = "123",
            id = newAdmin.Id
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

public partial class Program { }

// ✅ FUNCIÓN HELPER PARA GENERAR CÓDIGOS DE ESTUDIANTE
static async Task<string> GenerateStudentCodeAsync(ApplicationDbContext context)
{
    string studentCode;
    do
    {
        studentCode = "STU" + DateTime.Now.Year + Random.Shared.Next(1000, 9999);
    } while (await context.Students.AnyAsync(s => s.StudentCode == studentCode));

    return studentCode;
}