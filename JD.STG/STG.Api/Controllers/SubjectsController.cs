using Microsoft.AspNetCore.Mvc;
using STG.Api.Mappings;
using STG.Application.Services;
using STG.Contracts.Curriculum;

namespace STG.Api.Controllers;

[ApiController]
[Route("api/subjects")]
public sealed class SubjectsController : ControllerBase
{
    private readonly SubjectService _service;
    public SubjectsController(SubjectService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubjectDto>>> GetAll(CancellationToken ct)
        => Ok((await _service.ListAllAsync(ct)).Select(x => x.ToDto()));

    [HttpGet("by-area/{studyAreaId:guid}")]
    public async Task<ActionResult<IEnumerable<SubjectDto>>> GetByArea([FromRoute] Guid studyAreaId, CancellationToken ct)
        => Ok((await _service.ListByAreaAsync(studyAreaId, ct)).Select(x => x.ToDto()));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] SubjectCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var id = await _service.CreateAsync(req.Name, req.StudyAreaId, req.Code, req.IsElective, ct);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }

    [HttpPut("{id:guid}/rename")]
    public async Task<IActionResult> Rename([FromRoute] Guid id, [FromBody] SubjectRenameRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.RenameAsync(id, req.Name, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/move")]
    public async Task<IActionResult> Move([FromRoute] Guid id, [FromBody] SubjectMoveRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.MoveToAreaAsync(id, req.StudyAreaId, ct);
        return NoContent();
    }
}
