using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

public sealed class GroupService
{
    private readonly IGroupRepository _groups;
    private readonly IUnitOfWork _uow;

    public GroupService(IGroupRepository groups, IUnitOfWork uow)
    {
        _groups = groups;
        _uow = uow;
    }

    public Task<IReadOnlyList<Group>> ListByGradeAsync(Guid gradeId, CancellationToken ct = default)
        => _groups.ListByGradeAsync(gradeId, ct);

    public Task<Group?> GetByCodeAsync(Guid schoolYearId, string gradeName, string label, CancellationToken ct = default)
        => _groups.GetByCodeAsync(schoolYearId, gradeName, label, ct);

    public async Task<Guid> CreateAsync(Guid gradeId, string gradeName, string label, ushort size, CancellationToken ct = default)
    {
        var g = new Group(gradeId, gradeName, label, size);
        await _groups.AddAsync(g, ct);
        await _uow.SaveChangesAsync(ct);
        return g.Id;
    }

    public async Task ResizeAsync(Guid groupId, ushort newSize, CancellationToken ct = default)
    {
        var g = await _groups.GetByIdAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found.");
        g.Resize(newSize);
        _groups.Update(g);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task RelabelAsync(Guid groupId, string newLabel, CancellationToken ct = default)
    {
        var g = await _groups.GetByIdAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found.");
        g.Relabel(newLabel);
        _groups.Update(g);
        await _uow.SaveChangesAsync(ct);
    }
}
