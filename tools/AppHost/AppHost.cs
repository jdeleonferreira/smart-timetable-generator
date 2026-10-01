using System.Diagnostics;
using AppHost.Commands;
using Azure.Provisioning;
using Azure.Provisioning.AppService;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Configure the Azure App Service environment
builder.AddAzureAppServiceEnvironment("plan").ConfigureInfrastructure(infra =>
{
    var plan = infra.GetProvisionableResources()
        .OfType<AppServicePlan>()
        .Single();

    plan.Sku = new AppServiceSkuDescription
    {
        Name = "B1", // Basic tier, 1 core
    };
});

var sqlServer = builder
    .AddAzureSqlServer("sql")
    .RunAsContainer(container =>
    {
        // Configure SQL Server to run locally as a container
        container.WithLifetime(ContainerLifetime.Persistent);

        // Use SQL Server 2022 as the default of SQL Server 2025 doesn't work on Linux/MacOS
        container.WithImage("mssql/server:2022-latest");

        // Acepta la licencia (EULA) de SQL Server y usa la edición Developer (gratuita para desarrollo).
        // Aspire ya lo hace, pero se deja explícito para que el contenedor siempre arranque.
        container.WithEnvironment("ACCEPT_EULA", "Y");
        container.WithEnvironment("MSSQL_PID", "Developer");

        // If desired, set SQL Server Port to a constant value
        //container.WithHostPort(1800);
    });

var db = sqlServer
    .AddDatabase("SmartTimetable", "smart-timetable")
    .WithDropDatabaseCommand();

var migrationService = builder.AddProject<MigrationService>("migrations")
    .PublishAsAzureAppServiceWebsite((_, site) =>
    {
        const string envNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";

        // Needed for hosted service to run
        site.SiteConfig.IsAlwaysOn = true;

        // Dynamically set environment, so we can enable seeding of data (only happens in 'development')
        var environment = Environment.GetEnvironmentVariable(envNetCoreEnvironment);
        if (string.IsNullOrWhiteSpace(environment))
            return;

        var envSetting = new AppServiceNameValuePair { Name = envNetCoreEnvironment, Value = environment };
        site.SiteConfig.AppSettings.Add(new BicepValue<AppServiceNameValuePair>(envSetting));
    })
    .WithReference(db)
    .WaitFor(sqlServer);

var api = builder
    .AddProject<WebApi>("api")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitForCompletion(migrationService);

// Cliente web (React + Vite). Vite reenvía /api a la API, así que el navegador no necesita CORS.
// Solo en ejecución local; Aspire instala los paquetes de npm al arrancar.
if (!builder.ExecutionContext.IsPublishMode)
{
    builder.AddViteApp("web", "../../src/WebClient")
        .WithReference(api)
        .WaitFor(api)
        .WithEnvironment("API_URL", api.GetEndpoint("https"))
        .WithExternalHttpEndpoints()
        // Abre el cliente web en el navegador cuando está listo (en CI no hay navegador que abrir).
        .OnResourceReady((web, _, _) =>
        {
            if (Environment.GetEnvironmentVariable("CI") is null)
            {
                var url = web.GetEndpoint("http").Url;
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }

            return Task.CompletedTask;
        });
}

// Configure Application Insights and Log Analytics only if in publish mode
// When running locally, use Aspire Dashboard instead
if (builder.ExecutionContext.IsPublishMode)
{
    var logAnalytics = builder.AddAzureLogAnalyticsWorkspace("log-analytics");
    var insights = builder.AddAzureApplicationInsights("insights", logAnalytics);
    api.WithReference(insights);
    migrationService.WithReference(insights);
}

builder.Build().Run();