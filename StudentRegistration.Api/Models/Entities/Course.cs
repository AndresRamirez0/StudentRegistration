using System.ComponentModel.DataAnnotations;

namespace StudentRegistration.Api.Models.Entities
{
    public class Course
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public int Credits { get; set; } = 3; // Cada materia vale 3 créditos
        
        [Required]
        public int ProfessorId { get; set; }
        
        // Navegación
        public virtual Professor Professor { get; set; } = null!;
        public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    }
}