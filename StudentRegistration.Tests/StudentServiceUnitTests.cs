using Xunit;
using FluentAssertions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Mappings;  // ✅ CORREGIDO: Mappings con 's'
using StudentRegistration.Api.Models.DTOs;
using StudentRegistration.Api.Models.Entities;
using StudentRegistration.Api.Services;

namespace StudentRegistration.Tests
{
    public class StudentServiceUnitTests
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly StudentService _studentService;

        public StudentServiceUnitTests()
        {
            // Configurar base de datos en memoria
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new ApplicationDbContext(options);
            
            // Configurar AutoMapper
            var configuration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = configuration.CreateMapper();
            
            _studentService = new StudentService(_context, _mapper);
        }

        [Fact]
        public async Task CreateStudentAsync_WithValidData_ShouldCreateStudent()
        {
            // Arrange
            var createStudentDto = new CreateStudentDto
            {
                FirstName = "Ana",
                LastName = "García",
                Email = "ana.garcia@test.com"
            };

            // Act
            var result = await _studentService.CreateStudentAsync(createStudentDto);

            // Assert
            result.Should().NotBeNull();
            result.FirstName.Should().Be("Ana");
            result.LastName.Should().Be("García");
            result.Email.Should().Be("ana.garcia@test.com");
            result.StudentCode.Should().StartWith("STU");
            result.RegistrationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task CreateStudentAsync_WithDuplicateEmail_ShouldThrowException()
        {
            // Arrange
            var student = new Student
            {
                FirstName = "Test",
                LastName = "User",
                Email = "duplicate@test.com",
                StudentCode = "STU20241001",
                RegistrationDate = DateTime.UtcNow
            };
            
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();

            var createStudentDto = new CreateStudentDto
            {
                FirstName = "Another",
                LastName = "User",
                Email = "duplicate@test.com"
            };

            // Act & Assert
            var act = async () => await _studentService.CreateStudentAsync(createStudentDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Ya existe un estudiante con ese email");
        }

        [Fact]
        public async Task GetStudentByIdAsync_WithValidId_ShouldReturnStudent()
        {
            // Arrange
            var student = new Student
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                StudentCode = "STU20241002",
                RegistrationDate = DateTime.UtcNow
            };
            
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();

            // Act
            var result = await _studentService.GetStudentByIdAsync(student.Id);

            // Assert
            result.Should().NotBeNull();
            result!.FirstName.Should().Be("Test");
            result.LastName.Should().Be("User");
        }

        [Fact]
        public async Task GetStudentByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Act
            var result = await _studentService.GetStudentByIdAsync(99999);

            // Assert
            result.Should().BeNull();
        }
    }
}                   