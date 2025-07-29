namespace StudentRegistration.Api.Models.DTOs
{
    public class UpdateStudentDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        // Para actualizar también el usuario
        public string? Username { get; set; }
        public string? NewPassword { get; set; }
    }
}