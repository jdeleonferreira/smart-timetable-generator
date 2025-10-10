using STG.Application.Interfaces;
using STG.Domain.Entities;
using STG.Domain.ValueObjects;

namespace STG.Application.Services;

public class SchedulerService
{
    private readonly ICurriculumRepository _curriculum;
    private readonly ITeacherRepository _teachers;
    private readonly IRoomRepository _rooms;
    private readonly IGroupRepository _groups;
    private readonly ITimetableRepository _timetables;
    private readonly IUnitOfWork _uow;

    public SchedulerService(
        ICurriculumRepository curriculum,
        ITeacherRepository teachers,
        IRoomRepository rooms,
        IGroupRepository groups,
        ITimetableRepository timetables,
        IUnitOfWork uow)
    {
        _curriculum = curriculum;
        _teachers = teachers;
        _rooms = rooms;
        _groups = groups;
        _timetables = timetables;
        _uow = uow;
    }

    public async Task<Timetable> GenerateAsync(int year, WeekConfig week, CancellationToken ct = default)
    {
        var existing = await _timetables.GetByYearAsync(year, ct);
        if (existing is not null) return existing;

        var tt = new Timetable(year, week);
        var lines = await _curriculum.GetByYearAsync(year, ct);
        var groupsAll = await _groups.GetAllAsync(ct);
        var roomsAll = await _rooms.GetAllAsync(ct);
        var teachers = await _teachers.GetAllAsync(ct);

        var groupsByGrade = groupsAll.GroupBy(g => g.Grade).ToDictionary(g => g.Key, g => g.ToList());

        // Slots de la semana
        var weekSlots = week.Days
            .SelectMany(d => Enumerable.Range(1, week.BlocksPerDay).Select(b => TimeSlot.Of(d, b)))
            .ToList();

        // ------------------ NUEVO: ocupación ------------------
        var occTeacher = new HashSet<(string teacher, DayOfWeek day, int block)>();
        var occGroup = new HashSet<(string group, DayOfWeek day, int block)>();
        var occRoom = new HashSet<(string room, DayOfWeek day, int block)>();
        // ------------------------------------------------------

        foreach (var line in lines)
        {
            if (!groupsByGrade.TryGetValue(line.Grade, out var gradeGroups)) continue;

            foreach (var group in gradeGroups)
            {
                var teacher = teachers.FirstOrDefault(t => t.Subjects.Contains(line.Subject));
                if (teacher is null) continue;

                var room = roomsAll.FirstOrDefault(r => r.Capacity >= group.Size);
                if (room is null) continue;

                var remaining = line.WeeklyBlocks;

                // intenta colocar "remaining" bloques para ese grupo
                foreach (var slot in weekSlots)
                {
                    if (remaining <= 0) break;

                    var keyT = (teacher.Name, slot.Day, slot.Block);
                    var keyG = (group.Code, slot.Day, slot.Block);
                    var keyR = (room.Name, slot.Day, slot.Block);

                    // Evitar llamada que chocará
                    if (occTeacher.Contains(keyT)) continue;
                    if (occGroup.Contains(keyG)) continue;
                    if (occRoom.Contains(keyR)) continue;

                    var a = new Assignment(
                        groupCode: group.Code,
                        subject: line.Subject,
                        teacher: teacher.Name,
                        room: room.Name,
                        slot: slot,
                        blocks: 1
                    );

                    // Si llegamos aquí, no hay choques "conocidos"
                    tt.AddAssignment(a);

                    // Marcar ocupación
                    occTeacher.Add(keyT);
                    occGroup.Add(keyG);
                    occRoom.Add(keyR);

                    remaining--;
                }
            }
        }

        await _timetables.AddAsync(tt, ct);
        await _uow.SaveChangesAsync(ct);
        return tt;
    }

}
