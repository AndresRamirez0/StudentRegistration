namespace StudentRegistration.Api.Models.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public int TotalCredits { get; set; }
        
        // Informaci�n del usuario relacionado (para la edici�n)
        public string? Username { get; set; }
        public int? UserId { get; set; }
        
        public List<CourseDto> Courses { get; set; } = new();
    }
}