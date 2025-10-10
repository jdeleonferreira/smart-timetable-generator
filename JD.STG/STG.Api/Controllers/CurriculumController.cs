using Microsoft.AspNetCore.Mvc;
using STG.Api.DTOs;
using STG.Application.Services;
using STG.Domain.Entities;

[ApiController]
[Route("api/[controller]")]
public class CurriculumController : ControllerBase
{
    private readonly CurriculumService _service;
    public CurriculumController(CurriculumService service) => _service = service;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromBody] List<CurriculumLineRequest> lines, CancellationToken ct)
    {
        if (lines.Count == 0) return BadRequest(new ProblemDetails { Title = "Empty payload" });

        var entities = lines.Select(l => new StudyPlanEntry(l.Year, l.Grade, l.Subject, l.WeeklyBlocks));
        await _service.UploadAsync(entities, ct);
        return Ok();
    }

    [HttpGet("{year:int}")]
    public async Task<ActionResult<List<CurriculumLineResponse>>> GetByYear(int year, CancellationToken ct)
    {
        var list = await _service.GetByYearAsync(year, ct);
        return Ok(list.Select(l => new CurriculumLineResponse(l.Year, l.Grade, l.Subject, l.WeeklyBlocks)).ToList());
    }
}
