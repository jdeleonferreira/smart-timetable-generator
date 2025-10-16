// src/STG.Api/Controllers/Scheduling/AssignmentsController.cs
using Microsoft.AspNetCore.Mvc;
using STG.Api.DTOs;
using STG.Application.Services;

namespace STG.Api.Controllers;

[ApiController]
[Route("api/assignments")]
public sealed class AssignmentsController : ControllerBase
{
    private readonly AssignmentService _service;
    public AssignmentsController(AssignmentService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] AssignmentUpsertRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.UpsertAsync(req.GroupId, req.SubjectId, req.Year, req.WeeklyHours, req.TeacherId, req.Notes, ct);
        return NoContent();
    }

    [HttpPut("teacher")]
    public async Task<IActionResult> SetTeacher([FromQuery] Guid groupId, [FromQuery] Guid subjectId, [FromQuery] int year,
        [FromBody] AssignmentSetTeacherRequest req, CancellationToken ct)
    { await _service.SetTeacherAsync(groupId, subjectId, year, req.TeacherId, ct); return NoContent(); }

    [HttpPut("hours")]
    public async Task<IActionResult> SetHours([FromQuery] Guid groupId, [FromQuery] Guid subjectId, [FromQuery] int year,
        [FromBody] AssignmentSetHoursRequest req, CancellationToken ct)
    { await _service.SetWeeklyHoursAsync(groupId, subjectId, year, req.WeeklyHours, ct); return NoContent(); }

    [HttpDelete]
    public async Task<IActionResult> Remove([FromQuery] Guid groupId, [FromQuery] Guid subjectId, [FromQuery] int year, CancellationToken ct)
    { await _service.RemoveAsync(groupId, subjectId, year, ct); return NoContent(); }

    [HttpGet("by-teacher/{teacherId:guid}/{year:int}")]
    public async Task<IActionResult> ByTeacher(Guid teacherId, int year, CancellationToken ct)
        => Ok(await _service.ListByTeacherAsync(teacherId, year, ct));
}
