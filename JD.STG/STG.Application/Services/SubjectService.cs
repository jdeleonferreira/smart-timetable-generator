using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;
public sealed class SubjectService
{
    private readonly ISubjectRepository _subjects;
    private readonly IUnitOfWork _uow;

    public SubjectService(ISubjectRepository subjects, IUnitOfWork uow)
    {
        _subjects = subjects;
        _uow = uow;
    }

    public async Task<Guid> CreateAsync(string name, bool requiresLab = false, bool requiresDouble = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.");
        if (await _subjects.GetByNameAsync(name.Trim(), ct) is not null)
            throw new InvalidOperationException("Subject already exists.");

        var s = new Subject(name.Trim(), requiresLab, requiresDouble);
        await _subjects.AddAsync(s, ct);
        await _uow.SaveChangesAsync(ct);
        return s.Id;
    }

    public Task<Subject?> GetByNameAsync(string name, CancellationToken ct = default)
        => _subjects.GetByNameAsync(name, ct);

    public Task<IReadOnlyList<Subject>> ListAsync(CancellationToken ct = default)
        => _subjects.ListAllAsync(ct);

    public Task<IReadOnlyList<Subject>> SearchAsync(string prefix, int max = 20, CancellationToken ct = default)
        => _subjects.SearchByPrefixAsync(prefix, max, ct);

    public async Task RenameAsync(Guid id, string newName, CancellationToken ct = default)
    {
        var s = await _subjects.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Subject not found.");
        s.Rename(newName);
        _subjects.Update(s);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _subjects.GetByIdAsync(id, ct);
        _subjects.Remove(s);
        await _uow.SaveChangesAsync(ct);
    }
}