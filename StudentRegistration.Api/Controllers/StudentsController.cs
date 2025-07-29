using Microsoft.AspNetCore.Mvc;
using StudentRegistration.Api.Models.DTOs;
using StudentRegistration.Api.Services;

namespace StudentRegistration.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        /// <summary>
        /// Obtener todos los estudiantes
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentDto>>> GetAllStudents()
        {
            try
            {
                var students = await _studentService.GetAllStudentsAsync();
                return Ok(students);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener estudiante por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<StudentDto>> GetStudent(int id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                if (student == null)
                    return NotFound(new { message = "Estudiante no encontrado" });

                return Ok(student);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener información completa del estudiante para edición
        /// </summary>
        [HttpGet("{id}/edit-info")]
        public async Task<ActionResult<object>> GetStudentEditInfo(int id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                if (student == null)
                    return NotFound(new { message = "Estudiante no encontrado" });

                return Ok(student);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Crear nuevo estudiante
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<StudentDto>> CreateStudent(CreateStudentDto createStudentDto)
        {
            try
            {
                var student = await _studentService.CreateStudentAsync(createStudentDto);
                return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Actualizar estudiante
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<StudentDto>> UpdateStudent(int id, UpdateStudentDto updateStudentDto)
        {
            try
            {
                var student = await _studentService.UpdateStudentAsync(id, updateStudentDto);
                if (student == null)
                    return NotFound(new { message = "Estudiante no encontrado" });

                return Ok(student);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Eliminar estudiante
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteStudent(int id)
        {
            try
            {
                var result = await _studentService.DeleteStudentAsync(id);
                if (!result)
                    return NotFound(new { message = "Estudiante no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Inscribir estudiante en materias
        /// </summary>
        [HttpPost("enroll")]
        public async Task<ActionResult> EnrollStudent(CourseEnrollmentDto enrollmentDto)
        {
            try
            {
                var result = await _studentService.EnrollStudentInCoursesAsync(enrollmentDto);
                if (!result)
                    return BadRequest(new { message = "Error al inscribir al estudiante" });

                return Ok(new { message = "Estudiante inscrito exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener estudiantes por profesor
        /// </summary>
        [HttpGet("by-professor/{professorId}")]
        public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudentsByProfessor(int professorId)
        {
            try
            {
                var students = await _studentService.GetStudentsByProfessorAsync(professorId);
                return Ok(students);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener compañeros de clase para un estudiante en una materia específica
        /// </summary>
        [HttpGet("{studentId}/classmates/{courseId}")]
        public async Task<ActionResult<IEnumerable<ClassmateDto>>> GetClassmates(int studentId, int courseId)
        {
            try
            {
                var classmates = await _studentService.GetClassmatesAsync(studentId, courseId);
                return Ok(classmates);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener todos los compañeros de clase de un estudiante (en todas sus materias)
        /// </summary>
        [HttpGet("{studentId}/all-classmates")]
        public async Task<ActionResult<IEnumerable<StudentClassmatesDto>>> GetAllClassmates(int studentId)
        {
            try
            {
                var allClassmates = await _studentService.GetAllClassmatesAsync(studentId);
                return Ok(allClassmates);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}