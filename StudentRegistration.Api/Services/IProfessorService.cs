using StudentRegistration.Api.Models.DTOs;

namespace StudentRegistration.Api.Services
{
    public interface IProfessorService
    {
        Task<IEnumerable<ProfessorDto>> GetAllProfessorsAsync();
        Task<ProfessorDto?> GetProfessorByIdAsync(int id);
    }
}