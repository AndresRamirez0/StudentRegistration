using AutoMapper;
using StudentRegistration.Api.Models.DTOs;
using StudentRegistration.Api.Models.Entities;

namespace StudentRegistration.Api.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Student mappings
            CreateMap<Student, StudentDto>()
                .ForMember(dest => dest.Username, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Courses, opt => opt.MapFrom(src => 
                    src.StudentCourses.Select(sc => new CourseDto
                    {
                        Id = sc.Course.Id,
                        Name = sc.Course.Name,
                        Description = sc.Course.Description,
                        Credits = sc.Course.Credits,
                        Professor = new ProfessorDto
                        {
                            Id = sc.Course.Professor.Id,
                            FirstName = sc.Course.Professor.FirstName,
                            LastName = sc.Course.Professor.LastName,
                            Email = sc.Course.Professor.Email,
                            Department = sc.Course.Professor.Department
                        }
                    })));

            CreateMap<CreateStudentDto, Student>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.StudentCode, opt => opt.Ignore())
                .ForMember(dest => dest.RegistrationDate, opt => opt.Ignore())
                .ForMember(dest => dest.TotalCredits, opt => opt.Ignore())
                .ForMember(dest => dest.StudentCourses, opt => opt.Ignore());

            CreateMap<UpdateStudentDto, Student>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.StudentCode, opt => opt.Ignore())
                .ForMember(dest => dest.RegistrationDate, opt => opt.Ignore())
                .ForMember(dest => dest.TotalCredits, opt => opt.Ignore())
                .ForMember(dest => dest.StudentCourses, opt => opt.Ignore());

            // User mappings
            CreateMap<User, UserDto>();
            CreateMap<RegisterDto, User>();

            // Course mappings
            CreateMap<Course, CourseDto>()
                .ForMember(dest => dest.Professor, opt => opt.MapFrom(src => src.Professor));

            // Professor mappings
            CreateMap<Professor, ProfessorDto>();
        }
    }
}