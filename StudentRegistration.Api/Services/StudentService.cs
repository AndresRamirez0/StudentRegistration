using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Models.DTOs;
using StudentRegistration.Api.Models.Entities;

namespace StudentRegistration.Api.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public StudentService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _context.Students
                .Include(s => s.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                        .ThenInclude(c => c.Professor)
                .ToListAsync();

            return _mapper.Map<IEnumerable<StudentDto>>(students);
        }

        public async Task<StudentDto?> GetStudentByIdAsync(int id)
        {
            var student = await _context.Students
                .Include(s => s.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                        .ThenInclude(c => c.Professor)
                .FirstOrDefaultAsync(s => s.Id == id);

            return student == null ? null : _mapper.Map<StudentDto>(student);
        }

        public async Task<StudentDto> CreateStudentAsync(CreateStudentDto createStudentDto)
        {
            // Verificar que el email no exista
            var existingStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == createStudentDto.Email);
            
            if (existingStudent != null)
                throw new InvalidOperationException("Ya existe un estudiante con ese email");

            var student = _mapper.Map<Student>(createStudentDto);
            
            // Generar código único de estudiante
            string studentCode;
            do
            {
                studentCode = "STU" + DateTime.Now.Year + Random.Shared.Next(1000, 9999);
            } while (await _context.Students.AnyAsync(s => s.StudentCode == studentCode));
            
            student.StudentCode = studentCode;
            student.RegistrationDate = DateTime.UtcNow;

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return await GetStudentByIdAsync(student.Id) ?? throw new InvalidOperationException("Error al crear el estudiante");
        }

        public async Task<StudentDto?> UpdateStudentAsync(int id, UpdateStudentDto updateStudentDto)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return null;

            // Verificar que el email no exista en otro estudiante
            var existingStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == updateStudentDto.Email && s.Id != id);
            
            if (existingStudent != null)
                throw new InvalidOperationException("Ya existe otro estudiante con ese email");

            _mapper.Map(updateStudentDto, student);
            await _context.SaveChangesAsync();

            return await GetStudentByIdAsync(id);
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EnrollStudentInCoursesAsync(CourseEnrollmentDto enrollmentDto)
        {
            var student = await _context.Students
                .Include(s => s.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                        .ThenInclude(c => c.Professor)
                .FirstOrDefaultAsync(s => s.Id == enrollmentDto.StudentId);

            if (student == null)
                throw new InvalidOperationException("Estudiante no encontrado");

            // Validar que no tenga más de 3 materias
            if (enrollmentDto.CourseIds.Count > 3)
                throw new InvalidOperationException("No puede inscribirse en más de 3 materias");

            // Verificar que las materias existan
            var courses = await _context.Courses
                .Include(c => c.Professor)
                .Where(c => enrollmentDto.CourseIds.Contains(c.Id))
                .ToListAsync();

            if (courses.Count != enrollmentDto.CourseIds.Count)
                throw new InvalidOperationException("Una o más materias no existen");

            // Validar que no tenga clases con el mismo profesor
            var professorIds = courses.Select(c => c.ProfessorId).ToList();
            if (professorIds.Count != professorIds.Distinct().Count())
                throw new InvalidOperationException("No puede tener clases con el mismo profesor");

            // Limpiar inscripciones anteriores
            var existingEnrollments = await _context.StudentCourses
                .Where(sc => sc.StudentId == enrollmentDto.StudentId)
                .ToListAsync();
            _context.StudentCourses.RemoveRange(existingEnrollments);

            // Crear nuevas inscripciones
            foreach (var courseId in enrollmentDto.CourseIds)
            {
                var enrollment = new StudentCourse
                {
                    StudentId = enrollmentDto.StudentId,
                    CourseId = courseId,
                    EnrollmentDate = DateTime.UtcNow
                };
                _context.StudentCourses.Add(enrollment);
            }

            // Actualizar créditos totales
            student.TotalCredits = courses.Sum(c => c.Credits);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<StudentDto>> GetStudentsByProfessorAsync(int professorId)
        {
            var students = await _context.Students
                .Include(s => s.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                        .ThenInclude(c => c.Professor)
                .Where(s => s.StudentCourses.Any(sc => sc.Course.ProfessorId == professorId))
                .ToListAsync();

            return _mapper.Map<IEnumerable<StudentDto>>(students);
        }

        public async Task<IEnumerable<ClassmateDto>> GetClassmatesAsync(int studentId, int courseId)
        {
            // Verificar que el estudiante existe y está inscrito en la materia
            var studentExists = await _context.StudentCourses
                .AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

            if (!studentExists)
                throw new InvalidOperationException("El estudiante no está inscrito en esta materia");

            // Obtener compañeros de clase (excluyendo al estudiante mismo)
            var classmates = await _context.StudentCourses
                .Include(sc => sc.Student)
                .Where(sc => sc.CourseId == courseId && sc.StudentId != studentId)
                .Select(sc => new ClassmateDto
                {
                    Id = sc.Student.Id,
                    FirstName = sc.Student.FirstName,
                    LastName = sc.Student.LastName
                })
                .ToListAsync();

            return classmates;
        }

        public async Task<IEnumerable<StudentClassmatesDto>> GetAllClassmatesAsync(int studentId)
        {
            // Verificar que el estudiante existe
            var student = await _context.Students
                .Include(s => s.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                        .ThenInclude(c => c.Professor)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                throw new InvalidOperationException("Estudiante no encontrado");

            var result = new List<StudentClassmatesDto>();

            foreach (var enrollment in student.StudentCourses)
            {
                var classmates = await GetClassmatesAsync(studentId, enrollment.CourseId);
                
                result.Add(new StudentClassmatesDto
                {
                    CourseName = enrollment.Course.Name,
                    ProfessorName = $"{enrollment.Course.Professor.FirstName} {enrollment.Course.Professor.LastName}",
                    Classmates = classmates.ToList()
                });
            }

            return result;
        }
    }
}