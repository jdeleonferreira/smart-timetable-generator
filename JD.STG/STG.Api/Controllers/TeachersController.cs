using Microsoft.AspNetCore.Mvc;
using STG.Api.DTOs;
using STG.Api.Mappings;
using STG.Application.Services;

namespace STG.Api.Controllers;

[ApiController]
[Route("api/teachers")]
public sealed class TeachersController : ControllerBase
{
    private readonly TeacherService _service;
    public TeachersController(TeacherService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeacherDto>>> GetAll([FromQuery] bool onlyActive, CancellationToken ct)
        => Ok((await _service.ListAsync(onlyActive, ct)).Select(x => x.ToDto()));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] TeacherCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var id = await _service.CreateAsync(req.FullName, req.Email, req.MaxWeeklyLoad, ct);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }

    [HttpPut("{id:guid}/rename")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] TeacherRenameRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.RenameAsync(id, req.FullName, ct); return NoContent();
    }

    [HttpPut("{id:guid}/email")]
    public async Task<IActionResult> SetEmail(Guid id, [FromBody] TeacherEmailRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.SetEmailAsync(id, req.Email, ct); return NoContent();
    }

    [HttpPut("{id:guid}/max-load")]
    public async Task<IActionResult> SetMaxLoad(Guid id, [FromBody] TeacherMaxLoadRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.SetMaxWeeklyLoadAsync(id, req.MaxWeeklyLoad, ct); return NoContent();
    }

    [HttpPut("{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => Toggle(id, true, ct);
    [HttpPut("{id:guid}/deactivate")]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => Toggle(id, false, ct);

    private async Task<IActionResult> Toggle(Guid id, bool active, CancellationToken ct)
    {
        if (active) await _service.ActivateAsync(id, ct); else await _service.DeactivateAsync(id, ct);
        return NoContent();
    }
}
