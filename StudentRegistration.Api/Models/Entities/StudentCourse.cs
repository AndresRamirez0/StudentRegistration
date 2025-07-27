using System.ComponentModel.DataAnnotations;

namespace StudentRegistration.Api.Models.Entities
{
    public class StudentCourse
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public int CourseId { get; set; }
        
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public virtual Student Student { get; set; } = null!;
        public virtual Course Course { get; set; } = null!;
    }
}