using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STG.Application.Abstractions.Persistence;
using STG.Infrastructure.Persistence.Repositories;

namespace STG.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        //Reduce overhead en alta concurrencia.
        services.AddDbContextPool<StgDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("Default")));


        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IStudyPlanRepository, StudyPlanRepository>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<ITimetableRepository, TimetableRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();


        return services;
    }
}
