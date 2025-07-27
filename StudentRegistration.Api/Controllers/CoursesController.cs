using Microsoft.AspNetCore.Mvc;
using StudentRegistration.Api.Models.DTOs;
using StudentRegistration.Api.Services;

namespace StudentRegistration.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        /// <summary>
        /// Obtener todas las materias
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetAllCourses()
        {
            try
            {
                var courses = await _courseService.GetAllCoursesAsync();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener materia por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseDto>> GetCourse(int id)
        {
            try
            {
                var course = await _courseService.GetCourseByIdAsync(id);
                if (course == null)
                    return NotFound(new { message = "Materia no encontrada" });

                return Ok(course);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener materias disponibles para un estudiante
        /// </summary>
        [HttpGet("available/{studentId}")]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetAvailableCourses(int studentId)
        {
            try
            {
                var courses = await _courseService.GetAvailableCoursesForStudentAsync(studentId);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener materias por profesor
        /// </summary>
        [HttpGet("by-professor/{professorId}")]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCoursesByProfessor(int professorId)
        {
            try
            {
                var courses = await _courseService.GetCoursesByProfessorAsync(professorId);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}