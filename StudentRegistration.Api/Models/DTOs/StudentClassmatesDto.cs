namespace StudentRegistration.Api.Models.DTOs
{
    public class StudentClassmatesDto
    {
        public string CourseName { get; set; } = string.Empty;
        public string ProfessorName { get; set; } = string.Empty;
        public List<ClassmateDto> Classmates { get; set; } = new List<ClassmateDto>();
    }
}