using StudentRegistration.Api.Models.DTOs;

namespace StudentRegistration.Api.Services
{
    public interface IAuthService
    {
        Task<SimpleLoginResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<UserDto?> GetUserByUsernameAsync(string username);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<bool> UserExistsAsync(string username, string email);
        Task<bool> ValidateUserAsync(string username, string password);
    }
}