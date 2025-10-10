using STG.Api.Errors;
using STG.Application;
using STG.Infrastructure;
using STG.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Controllers + ProblemDetails para ModelState 400
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        // Mantén el comportamiento por defecto de [ApiController]:
        // Devolverá ValidationProblemDetails (400) cuando ModelState sea inválido.
        o.SuppressModelStateInvalidFilter = false;
    });

// OpenAPI (Aspire)
builder.Services.AddOpenApi();

// CORS (ajusta orígenes según tu front)
builder.Services.AddCors(o => o.AddPolicy("front", p =>
    p.WithOrigins("http://localhost:5173", "http://localhost:4200")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()
));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();

// 1) Global error handling FIRST
app.UseMiddleware<ErrorHandlingMiddleware>();

// 2) HTTP + CORS
app.UseHttpsRedirection();
app.UseCors("front");

// 3) AuthN/Z (si la agregaras más adelante)
app.UseAuthorization();

// OpenAPI solo en Dev (Aspire expone /openapi)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Smart Timetable Generator API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient) 
        .WithTheme(ScalarTheme.Moon)// opcional
        .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    })
    .WithDisplayName("API Reference")
    .WithGroupName("API Reference");

    // Seeder
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StgDbContext>();
    await DataSeeder.SeedAsync(db);
}

// Endpoints
app.MapControllers();

app.Run();
