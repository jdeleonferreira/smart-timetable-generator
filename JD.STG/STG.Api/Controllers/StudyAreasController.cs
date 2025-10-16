using Microsoft.AspNetCore.Mvc;
using STG.Api.DTOs;
using STG.Api.Mappings;
using STG.Application.Services;

namespace STG.Api.Controllers.Curriculum;

[ApiController]
[Route("api/study-areas")]
public sealed class StudyAreasController : ControllerBase
{
    private readonly StudyAreaService _service;
    public StudyAreasController(StudyAreaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudyAreaDto>>> GetAll(CancellationToken ct)
        => Ok((await _service.ListAsync(ct)).Select(x => x.ToDto()));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] StudyAreaCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var id = await _service.CreateAsync(req.Name, req.Code, req.OrderNo, ct);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] StudyAreaUpdateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.RenameAsync(id, req.Name, ct);
        // Update code / order / active
        var current = await _service.ListAsync(ct).ContinueWith(t => t.Result.FirstOrDefault(a => a.Id == id), ct);
        if (current is null) return NotFound();

        current.Recode(req.Code);
        current.Reorder(req.OrderNo);
        if (req.IsActive) current.Activate(); else current.Deactivate();

        // repo auto-save vía service.Update
        await _service.RenameAsync(id, current.Name, ct); // ya renombró; para no exponer un método Update completo, puedes usar repos directos si prefieres
        return NoContent();
    }
}