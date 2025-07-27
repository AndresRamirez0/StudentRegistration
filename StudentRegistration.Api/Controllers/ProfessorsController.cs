using Microsoft.AspNetCore.Mvc;
using StudentRegistration.Api.Models.DTOs;
using StudentRegistration.Api.Services;

namespace StudentRegistration.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfessorsController : ControllerBase
    {
        private readonly IProfessorService _professorService;

        public ProfessorsController(IProfessorService professorService)
        {
            _professorService = professorService;
        }

        /// <summary>
        /// Obtener todos los profesores
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProfessorDto>>> GetAllProfessors()
        {
            try
            {
                var professors = await _professorService.GetAllProfessorsAsync();
                return Ok(professors);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener profesor por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProfessorDto>> GetProfessor(int id)
        {
            try
            {
                var professor = await _professorService.GetProfessorByIdAsync(id);
                if (professor == null)
                    return NotFound(new { message = "Profesor no encontrado" });

                return Ok(professor);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}