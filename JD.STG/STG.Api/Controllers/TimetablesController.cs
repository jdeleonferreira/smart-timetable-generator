using Microsoft.AspNetCore.Mvc;
using STG.Api.DTOs;
using STG.Api.Mappings;
using STG.Application.Abstractions.Persistence;
using STG.Application.Scheduling;

namespace STG.Api.Controllers.Scheduling;

[ApiController]
[Route("api/timetables")]
public sealed class TimetablesController : ControllerBase
{
    private readonly TimetableService _timetableService;
    private readonly ITimetableRepository _timetableRepository;

    public TimetablesController(TimetableService service, ITimetableRepository repo)
    { _timetableService = service; _timetableRepository = repo; }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] TimetableCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var id = await _timetableService.CreateAsync(req.GroupId, req.Year, req.Name, req.Notes, ct);
        return CreatedAtAction(nameof(Get), new { timetableId = id }, id);
    }

    [HttpGet("{timetableId:guid}")]
    public async Task<ActionResult<TimetableDto>> Get(Guid timetableId, CancellationToken ct)
    {
        var tt = await _timetableRepository.GetByIdAsync(timetableId, ct);
        return tt is null ? NotFound() : Ok(tt.ToDto());
    }

    [HttpPost("{timetableId:guid}/slots")]
    public async Task<ActionResult<Guid>> AddSlot(Guid timetableId, [FromBody] TimetableAddSlotRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var slotId = await _timetableService.AddSlotAsync(timetableId, req.AssignmentId, req.DayOfWeek, req.PeriodIndex, req.Span, req.Room, req.Notes, ct);
        return Ok(slotId);
    }

    [HttpPut("slots/{entryId:guid}/move")]
    public async Task<IActionResult> MoveSlot(Guid entryId, [FromBody] TimetableMoveSlotRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _timetableService.MoveSlotAsync(entryId, req.DayOfWeek, req.PeriodIndex, req.Span, ct);
        return NoContent();
    }

    [HttpPut("slots/{entryId:guid}/assignment")]
    public async Task<IActionResult> ChangeAssignment(Guid entryId, [FromBody] TimetableChangeAssignmentRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _timetableService.ChangeAssignmentAsync(entryId, req.AssignmentId, ct);
        return NoContent();
    }

    [HttpDelete("slots/{entryId:guid}")]
    public Task<IActionResult> RemoveSlot(Guid entryId, CancellationToken ct)
        => _timetableService.RemoveSlotAsync(entryId, ct).ContinueWith<IActionResult>(_ => NoContent(), ct);
}
