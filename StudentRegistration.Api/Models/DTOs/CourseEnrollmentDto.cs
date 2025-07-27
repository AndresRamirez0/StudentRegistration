namespace StudentRegistration.Api.Models.DTOs
{
    public class CourseEnrollmentDto
    {
        public int StudentId { get; set; }
        public List<int> CourseIds { get; set; } = new List<int>();
    }
}