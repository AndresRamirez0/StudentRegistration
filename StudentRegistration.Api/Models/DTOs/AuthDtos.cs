using System.ComponentModel.DataAnnotations;

namespace StudentRegistration.Api.Models.DTOs
{
    public class LoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        public string LastName { get; set; } = string.Empty;
        
        public string Role { get; set; } = "Student";
    }

    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserDto? User { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class SimpleLoginResponseDto
    {
        public bool IsAuthenticated { get; set; }
        public UserDto? User { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}