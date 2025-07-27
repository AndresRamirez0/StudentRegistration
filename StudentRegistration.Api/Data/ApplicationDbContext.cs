using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Models.Entities;

namespace StudentRegistration.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Professor> Professors { get; set; }
        public DbSet<StudentCourse> StudentCourses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraci�n de relaciones
            modelBuilder.Entity<StudentCourse>()
                .HasOne(sc => sc.Student)
                .WithMany(s => s.StudentCourses)
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentCourse>()
                .HasOne(sc => sc.Course)
                .WithMany(c => c.StudentCourses)
                .HasForeignKey(sc => sc.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Professor)
                .WithMany(p => p.Courses)
                .HasForeignKey(c => c.ProfessorId)
                .OnDelete(DeleteBehavior.Restrict);

            // �ndices �nicos
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Email)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.StudentCode)
                .IsUnique();

            modelBuilder.Entity<Professor>()
                .HasIndex(p => p.Email)
                .IsUnique();

            // Restricci�n �nica para evitar doble inscripci�n
            modelBuilder.Entity<StudentCourse>()
                .HasIndex(sc => new { sc.StudentId, sc.CourseId })
                .IsUnique();

            // Seed Data - 5 Profesores
            modelBuilder.Entity<Professor>().HasData(
                new Professor { Id = 1, FirstName = "Ana", LastName = "Garc�a", Email = "ana.garcia@university.edu", Department = "Matem�ticas" },
                new Professor { Id = 2, FirstName = "Carlos", LastName = "L�pez", Email = "carlos.lopez@university.edu", Department = "Ciencias" },
                new Professor { Id = 3, FirstName = "Mar�a", LastName = "Rodr�guez", Email = "maria.rodriguez@university.edu", Department = "Humanidades" },
                new Professor { Id = 4, FirstName = "Jos�", LastName = "Mart�nez", Email = "jose.martinez@university.edu", Department = "Ingenier�a" },
                new Professor { Id = 5, FirstName = "Laura", LastName = "Fern�ndez", Email = "laura.fernandez@university.edu", Department = "Tecnolog�a" }
            );

            // Seed Data - 10 Materias (2 por profesor)
            modelBuilder.Entity<Course>().HasData(
                new Course { Id = 1, Name = "�lgebra Lineal", Description = "Fundamentos de �lgebra lineal", Credits = 3, ProfessorId = 1 },
                new Course { Id = 2, Name = "C�lculo Diferencial", Description = "Introducci�n al c�lculo diferencial", Credits = 3, ProfessorId = 1 },
                new Course { Id = 3, Name = "F�sica General", Description = "Principios b�sicos de f�sica", Credits = 3, ProfessorId = 2 },
                new Course { Id = 4, Name = "Qu�mica Org�nica", Description = "Estudio de compuestos org�nicos", Credits = 3, ProfessorId = 2 },
                new Course { Id = 5, Name = "Literatura Espa�ola", Description = "An�lisis de textos literarios", Credits = 3, ProfessorId = 3 },
                new Course { Id = 6, Name = "Historia Universal", Description = "Eventos hist�ricos mundiales", Credits = 3, ProfessorId = 3 },
                new Course { Id = 7, Name = "Estructuras de Datos", Description = "Algoritmos y estructuras fundamentales", Credits = 3, ProfessorId = 4 },
                new Course { Id = 8, Name = "Bases de Datos", Description = "Dise�o y gesti�n de bases de datos", Credits = 3, ProfessorId = 4 },
                new Course { Id = 9, Name = "Programaci�n Web", Description = "Desarrollo de aplicaciones web", Credits = 3, ProfessorId = 5 },
                new Course { Id = 10, Name = "Redes de Computadores", Description = "Fundamentos de redes", Credits = 3, ProfessorId = 5 }
            );
        }
    }
}