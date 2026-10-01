using SmartTimetableGenerator.Application;
using SmartTimetableGenerator.Infrastructure;
using SmartTimetableGenerator.WebApi;
using SmartTimetableGenerator.WebApi.Endpoints;
using SmartTimetableGenerator.WebApi.Extensions;
using SmartTimetableGenerator.WebApi.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddCustomProblemDetails();

builder.Services.AddWebApi(builder.Configuration);
builder.Services.AddApplication();
builder.AddInfrastructure();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapOpenApi();
app.MapCustomScalarApiReference();
app.UseHealthChecks();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

app.MapAuthEndpoints();
app.MapCatalogEndpoints();
app.MapAreaAndGradeEndpoints();
app.MapTeacherEndpoints();
app.MapStudyPlanEndpoints();
app.MapTimetableEndpoints();
app.UseEventualConsistencyMiddleware();

app.MapDefaultEndpoints();
app.UseExceptionHandler();

app.Run();