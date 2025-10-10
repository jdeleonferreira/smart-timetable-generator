using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

public sealed class Assignment : Entity
{
    public Guid SchoolYearId { get; private set; }     // e.g., 2025
    public Guid GroupId { get; private set; }          // 6A, 7B...
    public Guid SubjectId { get; private set; }        // Matemáticas, Inglés...
    public Guid TeacherId { get; private set; }        // Profe García...
    public byte WeeklyHours { get; private set; }      // IH: horas/semana

    // EF
    private Assignment() { }

    public Assignment(Guid schoolYearId, Guid groupId, Guid subjectId, Guid teacherId, byte weeklyHours)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId required.");
        if (groupId == Guid.Empty) throw new ArgumentException("GroupId required.");
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId required.");
        if (teacherId == Guid.Empty) throw new ArgumentException("TeacherId required.");
        if (weeklyHours is 0 or > 10) throw new ArgumentOutOfRangeException(nameof(weeklyHours), "WeeklyHours must be 1..10.");

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        GroupId = groupId;
        SubjectId = subjectId;
        TeacherId = teacherId;
        WeeklyHours = weeklyHours;
    }

    public void ChangeTeacher(Guid newTeacherId)
    {
        if (newTeacherId == Guid.Empty) throw new ArgumentException("TeacherId required.");
        TeacherId = newTeacherId;
        Touch();
    }

    public void ChangeWeeklyHours(byte newWeeklyHours)
    {
        if (newWeeklyHours is 0 or > 10) throw new ArgumentOutOfRangeException(nameof(newWeeklyHours));
        WeeklyHours = newWeeklyHours;
        Touch();
    }
}
