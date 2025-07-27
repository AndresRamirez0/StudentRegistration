using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRegistration.Api;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Models.DTOs;
using System.Net;
using System.Net.Http.Json;

namespace StudentRegistration.Tests
{
    public class StudentsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public StudentsControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remover la configuración de base de datos existente
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Usar base de datos en memoria para pruebas
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase("TestDatabase"));
                });
            });
            
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetAllStudents_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/students");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateStudent_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var newStudent = new CreateStudentDto
            {
                FirstName = "Juan",
                LastName = "Pérez",
                Email = $"juan.perez.{Guid.NewGuid()}@test.com"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/students", newStudent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var createdStudent = await response.Content.ReadFromJsonAsync<StudentDto>();
            createdStudent.Should().NotBeNull();
            createdStudent!.FirstName.Should().Be("Juan");
            createdStudent.LastName.Should().Be("Pérez");
            createdStudent.StudentCode.Should().StartWith("STU");
        }

        [Fact]
        public async Task GetStudent_WithInvalidId_ShouldReturnNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/students/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}