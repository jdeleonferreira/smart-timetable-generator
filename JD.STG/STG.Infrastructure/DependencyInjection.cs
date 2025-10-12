using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STG.Application.Abstractions.AI;
using STG.Application.Abstractions.Persistence;
using STG.Infrastructure.AI;
using STG.Infrastructure.AI.OpenAI;
using STG.Infrastructure.AI.OrTools;
using STG.Infrastructure.Persistence;
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

        services.AddScoped<IGradeRepository, GradeRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        
        services.AddScoped<ISchoolYearRepository, SchoolYearRepository>();

        services.AddScoped<IStudyPlanRepository, StudyPlanRepository>();
        services.AddScoped<IStudyPlanEntryRepository,StudyPlanEntryRepository>();
        
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        
        services.AddScoped<ITimetableRepository, TimetableRepository>();
        services.AddScoped<ITimetableEntryRepository, TimetableEntryRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var aiSection = config.GetSection("AI");
        var useOptimizer = aiSection.GetSection("Optimizer").Value; // "OrTools" | "None"
        var useExplainer = aiSection.GetSection("Explainer").Value; // "OpenAI" | "None"

        if (string.Equals(useOptimizer, "OrTools", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<ISchedulingOptimizer, OrToolsSchedulingOptimizer>();
        else
            services.AddSingleton<ISchedulingOptimizer, NoOpSchedulingOptimizer>();

        if (string.Equals(useExplainer, "OpenAI", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IConstraintExplainer, OpenAIConstraintExplainer>();
        else
            services.AddSingleton<IConstraintExplainer, NullConstraintExplainer>();

        // Bind options (no necesitas Binder si solo las consumes vía IOptions<T>)
        services.Configure<OpenAIOptions>(aiSection.GetSection("OpenAI"));
        services.Configure<OptimizerOptions>(aiSection.GetSection("Limits"));

        return services;
    }
}
