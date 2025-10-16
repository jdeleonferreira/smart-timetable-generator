using Microsoft.AspNetCore.Mvc;
using STG.Api.DTOs;
using STG.Api.Mappings;
using STG.Application.Services;

namespace STG.Api.Controllers;

[ApiController]
[Route("api/study-plans")]
public sealed class StudyPlansController : ControllerBase
{
    private readonly StudyPlanService _plans;
    private readonly StudyPlanEntryService _entries;

    public StudyPlansController(StudyPlanService plans, StudyPlanEntryService entries)
    { _plans = plans; _entries = entries; }

    [HttpGet("{year:int}")]
    public async Task<ActionResult<StudyPlanDto>> GetByYear(int year, CancellationToken ct)
    {
        var plan = await _plans.GetByYearAsync(year, ct);
        return plan is null ? NotFound() : Ok(plan.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateForYear([FromBody] StudyPlanCreateForYearRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var id = await _plans.CreateForYearAsync(req.Year, req.Name, req.Notes, ct);
        return CreatedAtAction(nameof(GetByYear), new { year = req.Year }, id);
    }

    [HttpPut("{year:int}/entries")]
    public async Task<IActionResult> UpsertEntry([FromRoute] int year, [FromBody] StudyPlanUpsertEntryRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _plans.UpsertEntryAsync(year, req.GradeId, req.SubjectId, req.WeeklyHours, req.Notes, ct);
        return NoContent();
    }

    [HttpDelete("{year:int}/entries")]
    public async Task<IActionResult> RemoveEntry([FromRoute] int year, [FromBody] StudyPlanRemoveEntryRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _plans.RemoveEntryAsync(year, req.GradeId, req.SubjectId, ct);
        return NoContent();
    }
}