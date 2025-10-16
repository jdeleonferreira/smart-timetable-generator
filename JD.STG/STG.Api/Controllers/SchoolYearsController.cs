using Microsoft.AspNetCore.Mvc;
using STG.Api.DTOs;
using STG.Api.Mappings;
using STG.Application.Services;

namespace STG.Api.Controllers;


[ApiController]
[Route("api/school-years")]
public sealed class SchoolYearsController : ControllerBase
{
    private readonly SchoolYearService _yearService;
    public SchoolYearsController(SchoolYearService yearsService) => _yearService = yearsService;

    [HttpGet("{year:int}")]
    public async Task<ActionResult<SchoolYearDto>> Get(int year, CancellationToken ct)
    {
        var sy = await _yearService.GetByYearAsync(year, ct);
        return sy is null ? NotFound() : Ok(sy.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] SchoolYearCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var id = await _yearService.CreateAsync(req.Year, ct);
        return CreatedAtAction(nameof(Get), new { year = req.Year }, id);
    }
}