namespace StudentRegistration.Api.Models.DTOs
{
    public class CourseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Credits { get; set; }
        public ProfessorDto Professor { get; set; } = new ProfessorDto();
        public List<string> EnrolledStudents { get; set; } = new List<string>();
    }
}