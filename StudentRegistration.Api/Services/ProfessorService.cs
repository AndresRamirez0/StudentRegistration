using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Models.DTOs;

namespace StudentRegistration.Api.Services
{
    public class ProfessorService : IProfessorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ProfessorService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProfessorDto>> GetAllProfessorsAsync()
        {
            var professors = await _context.Professors
                .Include(p => p.Courses)
                    .ThenInclude(c => c.StudentCourses)
                        .ThenInclude(sc => sc.Student)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProfessorDto>>(professors);
        }

        public async Task<ProfessorDto?> GetProfessorByIdAsync(int id)
        {
            var professor = await _context.Professors
                .Include(p => p.Courses)
                    .ThenInclude(c => c.StudentCourses)
                        .ThenInclude(sc => sc.Student)
                .FirstOrDefaultAsync(p => p.Id == id);

            return professor == null ? null : _mapper.Map<ProfessorDto>(professor);
        }
    }
}