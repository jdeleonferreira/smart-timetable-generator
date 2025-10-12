using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STG.Application.Services;

namespace STG.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {

        services.AddScoped<AssignmentService>();
        services.AddScoped<GradeService>();
        services.AddScoped<GroupService>();
        services.AddScoped<SchoolYearService>();
        services.AddScoped<StudyAreaService>();
        services.AddScoped<StudyPlanEntryService>();
        services.AddScoped<StudyPlanService>();
        services.AddScoped<SubjectService>();
        services.AddScoped<TeacherService>();

        return services;
    }
}
