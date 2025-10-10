using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

public sealed class TeacherService
{
    private readonly ITeacherRepository _teachers;
    private readonly IUnitOfWork _uow;

    public TeacherService(ITeacherRepository teachers, IUnitOfWork uow)
    {
        _teachers = teachers;
        _uow = uow;
    }

    public async Task<Guid> CreateAsync(string name, IEnumerable<string>? subjects = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.");
        if (await _teachers.GetByNameAsync(name.Trim(), ct) is not null)
            throw new InvalidOperationException("Teacher already exists.");

        var t = new Teacher(name.Trim(), subjects);
        await _teachers.AddAsync(t, ct);
        await _uow.SaveChangesAsync(ct);
        return t.Id;
    }

    public Task<IReadOnlyList<Teacher>> ListAsync(CancellationToken ct = default)
        => _teachers.ListAllAsync(ct);

    public Task<IReadOnlyList<Teacher>> ListQualifiedForAsync(string subjectName, CancellationToken ct = default)
        => _teachers.ListQualifiedForAsync(subjectName, ct);

    public async Task AddSubjectAsync(Guid teacherId, string subjectName, CancellationToken ct = default)
    {
        var t = await _teachers.GetByIdAsync(teacherId, ct) ?? throw new KeyNotFoundException("Teacher not found.");
        if (t.AddSubject(subjectName)) { _teachers.Update(t); await _uow.SaveChangesAsync(ct); }
    }

    public async Task RemoveSubjectAsync(Guid teacherId, string subjectName, CancellationToken ct = default)
    {
        var t = await _teachers.GetByIdAsync(teacherId, ct) ?? throw new KeyNotFoundException("Teacher not found.");
        if (t.RemoveSubject(subjectName)) { _teachers.Update(t); await _uow.SaveChangesAsync(ct); }
    }

    public async Task RenameAsync(Guid teacherId, string newName, CancellationToken ct = default)
    {
        var t = await _teachers.GetByIdAsync(teacherId, ct) ?? throw new KeyNotFoundException("Teacher not found.");
        t.Rename(newName);
        _teachers.Update(t);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid teacherId, CancellationToken ct = default)
    {
        var t = await _teachers.GetByIdAsync(teacherId, ct);
        _teachers.Remove(t);
        await _uow.SaveChangesAsync(ct);
    }
}
