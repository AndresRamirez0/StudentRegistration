using StudentRegistration.Api.Models.DTOs;

namespace StudentRegistration.Api.Services
{
    public interface IStudentService
    {
        Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
        Task<StudentDto?> GetStudentByIdAsync(int id);
        Task<StudentDto> CreateStudentAsync(CreateStudentDto createStudentDto);
        Task<StudentDto?> UpdateStudentAsync(int id, UpdateStudentDto updateStudentDto);
        Task<bool> DeleteStudentAsync(int id);
        Task<bool> EnrollStudentInCoursesAsync(CourseEnrollmentDto enrollmentDto);
        Task<IEnumerable<StudentDto>> GetStudentsByProfessorAsync(int professorId);
        Task<IEnumerable<ClassmateDto>> GetClassmatesAsync(int studentId, int courseId);
        Task<IEnumerable<StudentClassmatesDto>> GetAllClassmatesAsync(int studentId);
    }
}