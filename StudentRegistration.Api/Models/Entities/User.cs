using System.ComponentModel.DataAnnotations;

namespace StudentRegistration.Api.Models.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Student"; // Student, Admin, Professor
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Relación opcional con Student (si el usuario es estudiante)
        public int? StudentId { get; set; }
        public virtual Student? Student { get; set; }
        
        // Relación opcional con Professor (si el usuario es profesor)
        public int? ProfessorId { get; set; }
        public virtual Professor? Professor { get; set; }
    }
}