using Microsoft.AspNetCore.Mvc;
using StudentRegistration.Api.Models.DTOs;
using StudentRegistration.Api.Services;

namespace StudentRegistration.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Iniciar sesión simple (sin token)
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<SimpleLoginResponseDto>> Login(LoginDto loginDto)
        {
            try
            {
                var response = await _authService.LoginAsync(loginDto);
                
                if (!response.IsAuthenticated)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new SimpleLoginResponseDto
                {
                    IsAuthenticated = false,
                    Message = $"Error en el login: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Registrar nuevo usuario simple
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            try
            {
                var response = await _authService.RegisterAsync(registerDto);
                
                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return CreatedAtAction(nameof(GetUserByUsername), 
                    new { username = registerDto.Username }, response);
            }
            catch (Exception ex)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = $"Error en el registro: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obtener usuario por nombre de usuario
        /// </summary>
        [HttpGet("user/{username}")]
        public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
        {
            try
            {
                var user = await _authService.GetUserByUsernameAsync(username);
                
                if (user == null)
                    return NotFound(new { message = "Usuario no encontrado" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cambiar contraseña (requiere ID de usuario)
        /// </summary>
        [HttpPost("change-password/{userId}")]
        public async Task<ActionResult> ChangePassword(int userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
                
                if (!result)
                    return BadRequest(new { message = "Contraseña actual incorrecta o usuario no encontrado" });

                return Ok(new { message = "Contraseña cambiada exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Validar credenciales
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult> ValidateCredentials(LoginDto loginDto)
        {
            try
            {
                var isValid = await _authService.ValidateUserAsync(loginDto.Username, loginDto.Password);
                
                return Ok(new { 
                    isValid = isValid, 
                    message = isValid ? "Credenciales válidas" : "Credenciales inválidas" 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}