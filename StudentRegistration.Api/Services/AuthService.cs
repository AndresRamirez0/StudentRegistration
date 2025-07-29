using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Models.DTOs;
using StudentRegistration.Api.Models.Entities;

namespace StudentRegistration.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AuthService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<SimpleLoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Professor)
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return new SimpleLoginResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Usuario o contraseña incorrectos"
                };
            }

            // Actualizar último login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var userDto = _mapper.Map<UserDto>(user);

            return new SimpleLoginResponseDto
            {
                IsAuthenticated = true,
                User = userDto,
                Message = "Login exitoso"
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // Verificar si ya existe el usuario
            if (await UserExistsAsync(registerDto.Username, registerDto.Email))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "El usuario o email ya existe"
                };
            }

            if (string.IsNullOrWhiteSpace(registerDto.Password))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "La contraseña no puede estar vacía"
                };
            }

            // ✅ NORMALIZAR EL ROL PARA CONSISTENCIA
            var normalizedRole = string.IsNullOrWhiteSpace(registerDto.Role) ? "Student" : registerDto.Role.Trim();
            
            // ✅ NORMALIZAR A FORMATO CORRECTO (Primera letra mayúscula)
            if (normalizedRole.Equals("student", StringComparison.OrdinalIgnoreCase))
            {
                normalizedRole = "Student";
            }
            else if (normalizedRole.Equals("professor", StringComparison.OrdinalIgnoreCase))
            {
                normalizedRole = "Professor";
            }
            else if (normalizedRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                normalizedRole = "Admin";
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Role = normalizedRole, // ✅ USAR ROL NORMALIZADO
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // ✅ CREAR REGISTRO STUDENT PARA CUALQUIER VARIACIÓN DE "STUDENT"
            if (normalizedRole.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                var student = new Student
                {
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    Email = registerDto.Email,
                    StudentCode = await GenerateStudentCodeAsync(),
                    RegistrationDate = DateTime.UtcNow,
                    TotalCredits = 0
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                user.StudentId = student.Id;
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userDto = _mapper.Map<UserDto>(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Usuario registrado exitosamente",
                User = userDto
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Professor)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            return user == null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto?> GetUserByUsernameAsync(string username)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Professor)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            return user == null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
                return false;

            // ✅ SIN VALIDACIÓN DE NUEVA CONTRASEÑA - ACEPTA CUALQUIER CONTRASEÑA
            // Solo verificar que no esté vacía
            if (string.IsNullOrWhiteSpace(changePasswordDto.NewPassword))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UserExistsAsync(string username, string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Username == username || u.Email == email);
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            return user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        private async Task<string> GenerateStudentCodeAsync()
        {
            string studentCode;
            do
            {
                studentCode = "STU" + DateTime.Now.Year + Random.Shared.Next(1000, 9999);
            } while (await _context.Students.AnyAsync(s => s.StudentCode == studentCode));

            return studentCode;
        }
    }
}