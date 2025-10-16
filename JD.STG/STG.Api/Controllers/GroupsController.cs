using Microsoft.AspNetCore.Mvc;
using STG.Api.DTOs;
using STG.Api.Mappings;
using STG.Application.Services;

namespace STG.Api.Controllers;

[ApiController]
[Route("api/groups")]
public sealed class GroupsController : ControllerBase
{
    private readonly GroupService _service;
    public GroupsController(GroupService service) => _service = service;

    [HttpGet("by-grade/{gradeId:guid}")]
    public async Task<ActionResult<IEnumerable<GroupDto>>> GetByGrade(Guid gradeId, CancellationToken ct)
        => Ok((await _service.ListByGradeAsync(gradeId, ct)).Select(x => x.ToDto()));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] GroupCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var id = await _service.CreateAsync(req.GradeId, req.Name, ct);
        return CreatedAtAction(nameof(GetByGrade), new { gradeId = req.GradeId }, id);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IEnumerable<Guid>>> CreateBulk([FromBody] GroupBulkCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var ids = await _service.CreateBulkAsync(req.GradeId, req.Names, ct);
        return Ok(ids);
    }

    [HttpPut("{id:guid}/rename")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] GroupRenameRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.RenameAsync(id, req.Name, ct); return NoContent();
    }
}