using Microsoft.AspNetCore.Mvc;
using STG.Api.DTOs;
using STG.Application.Services;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly ResourceService _svc;
    public ResourcesController(ResourceService svc) => _svc = svc;

    // SUBJECTS
    [HttpPost("subjects/bulk")]
    public async Task<IActionResult> EnsureSubjects([FromBody] List<SubjectDto> subjects, CancellationToken ct)
    {
        await _svc.EnsureSubjectsAsync(subjects.Select(s => s.Name), ct);
        return Ok();
    }

    [HttpGet("subjects")]
    public async Task<ActionResult<List<SubjectDto>>> GetSubjects(CancellationToken ct)
    {
        var list = await _svc.GetSubjectsAsync(ct);
        return Ok(list.Select(s => new SubjectDto(s.Name, s.NeedsLab /*, s.NeedsComputerRoom*/ )).ToList());
    }

    // TEACHERS
    [HttpPost("teachers")]
    public async Task<IActionResult> AddTeacher([FromBody] CreateTeacherRequest req, CancellationToken ct)
    {
        await _svc.AddTeacherAsync(req.Name, req.Subjects, ct);
        return CreatedAtAction(nameof(GetTeachers), null);
    }

    [HttpGet("teachers")]
    public async Task<ActionResult<List<TeacherDto>>> GetTeachers(CancellationToken ct)
    {
        var list = await _svc.GetTeachersAsync(ct);
        return Ok(list.Select(t => new TeacherDto(t.Name, t.Subjects.ToList())).ToList());
    }

    // ROOMS
    [HttpPost("rooms")]
    public async Task<IActionResult> AddRoom([FromBody] CreateRoomRequest req, CancellationToken ct)
    {
        await _svc.AddRoomAsync(req.Name, req.Capacity, req.Tags, ct);
        return CreatedAtAction(nameof(GetRooms), null);
    }

    [HttpGet("rooms")]
    public async Task<ActionResult<List<RoomDto>>> GetRooms(CancellationToken ct)
    {
        var list = await _svc.GetRoomsAsync(ct);
        return Ok(list.Select(r => new RoomDto(r.Name, r.Capacity, r.Tags.Values.ToList())).ToList());
    }

    // GROUPS
    [HttpPost("groups")]
    public async Task<IActionResult> AddGroup([FromBody] CreateGroupRequest req, CancellationToken ct)
    {
        await _svc.AddGroupAsync(req.Grade, req.Label, req.Size, ct);
        return CreatedAtAction(nameof(GetGroups), null);
    }

    [HttpGet("groups")]
    public async Task<ActionResult<List<GroupDto>>> GetGroups(CancellationToken ct)
    {
        var list = await _svc.GetGroupsAsync(ct);
        return Ok(list.Select(g => new GroupDto(g.Grade, g.Label, g.Size, g.Code)).ToList());
    }
}
