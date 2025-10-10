using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STG.Application.Interfaces;
using STG.Infrastructure.Persistence;
using STG.Infrastructure.Repositories;

namespace STG.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        //Reduce overhead en alta concurrencia.
        services.AddDbContextPool<StgDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("Default")));


        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICurriculumRepository, CurriculumRepository>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<ITimetableRepository, TimetableRepository>();


        return services;
    }
}
