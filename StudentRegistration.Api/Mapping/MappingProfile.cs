using AutoMapper;
using StudentRegistration.Api.Models.DTOs;
using StudentRegistration.Api.Models.Entities;

namespace StudentRegistration.Api.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeos existentes
            CreateMap<Student, StudentDto>()
                .ForMember(dest => dest.Courses, opt => opt.MapFrom(src => src.StudentCourses.Select(sc => sc.Course)));
            
            CreateMap<Course, CourseDto>()
                .ForMember(dest => dest.EnrolledStudents, opt => opt.MapFrom(src => src.StudentCourses.Select(sc => $"{sc.Student.FirstName} {sc.Student.LastName}")));
            
            CreateMap<Professor, ProfessorDto>();

            // Nuevos mapeos necesarios para las pruebas
            CreateMap<CreateStudentDto, Student>();
            CreateMap<UpdateStudentDto, Student>();

            // NUEVOS MAPEOS PARA AUTENTICACIÓN
            CreateMap<User, UserDto>();
            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
        }
    }
}