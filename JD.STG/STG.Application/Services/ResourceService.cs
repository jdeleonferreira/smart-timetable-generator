using STG.Application.Interfaces;
using STG.Domain.Entities;

namespace STG.Application.Services;

public class ResourceService
{
    private readonly ITeacherRepository _teachers;
    private readonly IRoomRepository _rooms;
    private readonly IGroupRepository _groups;
    private readonly ISubjectRepository _subjects;
    private readonly IUnitOfWork _uow;

    public ResourceService(
        ITeacherRepository teachers,
        IRoomRepository rooms,
        IGroupRepository groups,
        ISubjectRepository subjects,
        IUnitOfWork uow)
    {
        _teachers = teachers;
        _rooms = rooms;
        _groups = groups;
        _subjects = subjects;
        _uow = uow;
    }

    // SUBJECTS
    public async Task EnsureSubjectsAsync(IEnumerable<string> subjectNames, CancellationToken ct = default)
    {
        var existing = await _subjects.GetAllAsync(ct);
        var existingSet = existing.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = subjectNames.Where(n => !existingSet.Contains(n))
                                .Select(n => new Subject(n));
        await _subjects.AddRangeAsync(toAdd, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default)
        => _subjects.GetAllAsync(ct);

    // TEACHERS
    public async Task AddTeacherAsync(string name, IEnumerable<string>? subjects, CancellationToken ct = default)
    {
        var t = new Teacher(name, subjects);
        await _teachers.AddAsync(t, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<Teacher>> GetTeachersAsync(CancellationToken ct = default)
        => _teachers.GetAllAsync(ct);

    // ROOMS
    public async Task AddRoomAsync(string name, int capacity, IEnumerable<string>? tags, CancellationToken ct = default)
    {
        var r = new Room(name, capacity, tags);
        await _rooms.AddAsync(r, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<Room>> GetRoomsAsync(CancellationToken ct = default)
        => _rooms.GetAllAsync(ct);

    // GROUPS
    public async Task AddGroupAsync(string grade, string label, int size, CancellationToken ct = default)
    {
        var g = new Group(grade, label, size);
        await _groups.AddAsync(g, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<Group>> GetGroupsAsync(CancellationToken ct = default)
        => _groups.GetAllAsync(ct);
}
