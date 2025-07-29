using StudentRegistration.Api.Models.DTOs;

namespace StudentRegistration.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<bool> UserExistsAsync(string username, string email);
        string GenerateJwtToken(UserDto user);
    }
}