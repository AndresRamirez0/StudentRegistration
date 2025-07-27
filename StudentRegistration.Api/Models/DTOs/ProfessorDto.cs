namespace StudentRegistration.Api.Models.DTOs
{
    public class ProfessorDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public List<CourseDto> Courses { get; set; } = new List<CourseDto>();
    }
}