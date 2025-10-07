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
        // Reusar si existe
        var existing = await _timetables.GetByYearAsync(year, ct);
        if (existing is not null) return existing;

        var tt = new Timetable(year, week);
        var lines = await _curriculum.GetByYearAsync(year, ct);
        var groups = await _groups.GetAllAsync(ct);
        var rooms = await _rooms.GetAllAsync(ct);
        var teachers = await _teachers.GetAllAsync(ct);

        // Map rápido de grupos por grado (asumimos 1 grupo por label, MVP)
        var groupsByGrade = groups.GroupBy(g => g.Grade).ToDictionary(g => g.Key, g => g.ToList());

        // Todos los slots de la semana
        var weekSlots = week.Days
            .SelectMany(d => Enumerable.Range(1, week.BlocksPerDay).Select(b => TimeSlot.Of(d, b)))
            .ToList();

        foreach (var line in lines)
        {
            if (!groupsByGrade.TryGetValue(line.Grade, out var gradeGroups)) continue;

            foreach (var group in gradeGroups)
            {
                int remaining = line.WeeklyBlocks;

                // Docente: el primero que dice poder esa materia (MVP)
                var teacher = teachers.FirstOrDefault(t => t.Subjects.Contains(line.Subject));
                if (teacher is null) continue;

                // Sala: primera con capacidad >= grupo.size (MVP)
                var room = rooms.FirstOrDefault(r => r.Capacity >= group.Size) ?? rooms.First();

                // Asignar greedy
                foreach (var slot in weekSlots)
                {
                    if (remaining <= 0) break;

                    var a = new Assignment(
                        groupCode: group.Code,
                        subject: line.Subject,
                        teacher: teacher.Name,
                        room: room.Name,
                        slot: slot,
                        blocks: 1 // MVP: 1 bloque. Puedes usar Subject.MustBeDouble más adelante
                    );

                    try
                    {
                        tt.AddAssignment(a);
                        remaining--;
                    }
                    catch
                    {
                        // conflicto -> prueba siguiente slot
                        continue;
                    }
                }
            }
        }

        await _timetables.AddAsync(tt, ct);
        await _uow.SaveChangesAsync(ct);
        return tt;
    }
}
