using StudentRegistration.Api.Models.DTOs;

namespace StudentRegistration.Api.Services
{
    public interface ICourseService
    {
        Task<IEnumerable<CourseDto>> GetAllCoursesAsync();
        Task<CourseDto?> GetCourseByIdAsync(int id);
        Task<IEnumerable<CourseDto>> GetAvailableCoursesForStudentAsync(int studentId);
        Task<IEnumerable<CourseDto>> GetCoursesByProfessorAsync(int professorId);
    }
}