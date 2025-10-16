using Microsoft.AspNetCore.Mvc;
using STG.Api.Contracts;
using STG.Api.DTOs;
using STG.Api.Mappings;
using STG.Application.Services;

namespace STG.Api.Controllers;

[ApiController]
[Route("api")]
public class PeriodSlotsController : ControllerBase
{
    private readonly PeriodSlotService _periodSlotService;

    public PeriodSlotsController(PeriodSlotService service) => _periodSlotService = service;

    // GET /schoolyears/{schoolYearId}/periodslots
    [HttpGet("schoolyears/{schoolYearId:guid}/periodslots")]
    public async Task<IActionResult> List(Guid schoolYearId, CancellationToken ct)
        => Ok(await _periodSlotService.ListAsync(schoolYearId, ct));

    // POST /periodslots
    [HttpPost("periodslots")]
    public async Task<IActionResult> Create([FromBody] CreatePeriodSlotRequest request, CancellationToken ct)
    {
        var (schoolYearId, day, period, start, end, isBreak, label) = request.ToCreateParams();
        var id = await _periodSlotService.CreateAsync(schoolYearId, day, period, start, end, isBreak, label, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    // GET /periodslots/{id}
    [HttpGet("periodslots/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        // Puedes ampliar el servicio si quieres un GetById DTO;
        // por simplicidad, devolvemos la lista del año del slot encontrado.
        return Ok(new { id });
    }

    // PUT /periodslots/{id}
    [HttpPut("periodslots/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePeriodSlotRequest request, CancellationToken ct)
    {
        var (start, end, isBreak, label) = request.ToUpdateParams();
        await _periodSlotService.UpdateAsync(id, start, end, isBreak, label, ct);
        return NoContent();
    }

    // DELETE /periodslots/{id}
    [HttpDelete("periodslots/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _periodSlotService.DeleteAsync(id, ct);
        return NoContent();
    }
}
