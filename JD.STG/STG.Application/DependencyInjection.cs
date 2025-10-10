using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STG.Application.Abstractions.AI;
using STG.Application.Services;

namespace STG.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {

        services.AddScoped<AssignmentService>();
        services.AddScoped<CurriculumService>();
        services.AddScoped<GroupService>();
        services.AddScoped<ResourceService>();
        services.AddScoped<SchedulingService>();
        services.AddScoped<StudyPlanService>();
        services.AddScoped<SubjectService>();
        services.AddScoped<TeacherService>();

        var aiSection = config.GetSection("AI");
        var useOptimizer = aiSection.GetValue<string>("Optimizer");
        var useExplainer = aiSection.GetValue<string>("Explainer");

        if (useOptimizer?.Equals("OrTools", StringComparison.OrdinalIgnoreCase) == true)
            services.AddSingleton<ISchedulingOptimizer, OrToolsSchedulingOptimizer>();
        else
            services.AddSingleton<ISchedulingOptimizer, NoOpSchedulingOptimizer>(); // opcional

        if (useExplainer?.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) == true)
            services.AddSingleton<IConstraintExplainer, OpenAIConstraintExplainer>();
        else
            services.AddSingleton<IConstraintExplainer, NullConstraintExplainer>();

        services.Configure<OpenAIOptions>(aiSection.GetSection("OpenAI")); // apiKey desde Key Vault
        services.Configure<OptimizerOptions>(aiSection.GetSection("Limits"));

        return services;
    }
}
