using Microsoft.AspNetCore.Mvc;
using STG.Api.DTOs;
using STG.Api.Mappings;
using STG.Application.Services;

namespace STG.Api.Controllers.Schools;

[ApiController]
[Route("api/grades")]
public sealed class GradesController : ControllerBase
{
    private readonly GradeService _service;
    public GradesController(GradeService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GradeDto>>> GetAll(CancellationToken ct)
        => Ok((await _service.ListAsync(ct)).Select(x => x.ToDto()));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] GradeCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var id = await _service.CreateAsync(req.Name, req.Order, ct);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }

    [HttpPut("{id:guid}/rename")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] GradeRenameRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.RenameAsync(id, req.Name, ct); return NoContent();
    }

    [HttpPut("{id:guid}/reorder")]
    public async Task<IActionResult> Reorder(Guid id, [FromBody] GradeReorderRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.ReorderAsync(id, req.Order, ct); return NoContent();
    }
}