using STG.Domain.Entities;

namespace STG.Application.Interfaces;

public interface ITeacherRepository
{
    Task<Teacher?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Teacher>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<Teacher>> GetQualifiedForAsync(string subject, CancellationToken ct);
    Task AddAsync(Teacher teacher, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Teacher> teachers, CancellationToken ct);
    void Remove(Teacher teacher);
}
