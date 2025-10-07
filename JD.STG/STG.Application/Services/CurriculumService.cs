using STG.Application.Interfaces;
using STG.Domain.Entities;

namespace STG.Application.Services;

public class CurriculumService
{
    private readonly ICurriculumRepository _curriculum;
    private readonly IUnitOfWork _uow;

    public CurriculumService(ICurriculumRepository curriculum, IUnitOfWork uow)
    {
        _curriculum = curriculum;
        _uow = uow;
    }

    public async Task UploadAsync(IEnumerable<CurriculumLine> items, CancellationToken ct = default)
    {
        var list = items.ToList();
        if (list.Count == 0) return;

        var year = list[0].Year;
        await _curriculum.ClearYearAsync(year, ct);
        await _curriculum.AddRangeAsync(list, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<CurriculumLine>> GetByYearAsync(int year, CancellationToken ct = default)
        => _curriculum.GetByYearAsync(year, ct);

    public Task<IReadOnlyList<CurriculumLine>> GetByGradeAsync(int year, string grade, CancellationToken ct = default)
        => _curriculum.GetByGradeAsync(year, grade, ct);

    public Task<IReadOnlyList<CurriculumLine>> GetBySubjectAsync(int year, string subject, CancellationToken ct = default)
        => _curriculum.GetBySubjectAsync(year, subject, ct);
}
