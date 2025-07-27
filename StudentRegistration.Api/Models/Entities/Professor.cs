using System.ComponentModel.DataAnnotations;

namespace StudentRegistration.Api.Models.Entities
{
    public class Professor
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;
        
        // Navegación - cada profesor dicta máximo 2 materias
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}