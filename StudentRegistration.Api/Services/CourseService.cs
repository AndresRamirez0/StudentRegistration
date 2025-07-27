using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Models.DTOs;

namespace StudentRegistration.Api.Services
{
    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CourseService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync()
        {
            var courses = await _context.Courses
                .Include(c => c.Professor)
                .Include(c => c.StudentCourses)
                    .ThenInclude(sc => sc.Student)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CourseDto>>(courses);
        }

        public async Task<CourseDto?> GetCourseByIdAsync(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Professor)
                .Include(c => c.StudentCourses)
                    .ThenInclude(sc => sc.Student)
                .FirstOrDefaultAsync(c => c.Id == id);

            return course == null ? null : _mapper.Map<CourseDto>(course);
        }

        public async Task<IEnumerable<CourseDto>> GetAvailableCoursesForStudentAsync(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                        .ThenInclude(c => c.Professor)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return new List<CourseDto>();

            // Obtener IDs de profesores que ya tiene el estudiante
            var enrolledProfessorIds = student.StudentCourses
                .Select(sc => sc.Course.ProfessorId)
                .ToList();

            // Obtener cursos disponibles (que no sean del mismo profesor)
            var availableCourses = await _context.Courses
                .Include(c => c.Professor)
                .Include(c => c.StudentCourses)
                    .ThenInclude(sc => sc.Student)
                .Where(c => !enrolledProfessorIds.Contains(c.ProfessorId))
                .ToListAsync();

            return _mapper.Map<IEnumerable<CourseDto>>(availableCourses);
        }

        public async Task<IEnumerable<CourseDto>> GetCoursesByProfessorAsync(int professorId)
        {
            var courses = await _context.Courses
                .Include(c => c.Professor)
                .Include(c => c.StudentCourses)
                    .ThenInclude(sc => sc.Student)
                .Where(c => c.ProfessorId == professorId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CourseDto>>(courses);
        }
    }
}