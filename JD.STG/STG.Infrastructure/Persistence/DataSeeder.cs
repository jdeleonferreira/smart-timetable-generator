using Microsoft.EntityFrameworkCore;
using STG.Domain.Entities;
using STG.Domain.ValueObjects;

namespace STG.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(StgDbContext db, CancellationToken ct = default)
    {
        // 1) SchoolYear 2025
        var year2025 = await db.SchoolYears.FirstOrDefaultAsync(x => x.Year == 2025, ct);
        if (year2025 is null)
        {
            year2025 = new SchoolYear(
                year: 2025,
                week: new WeekConfig(
                    days: new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                    blocksPerDay: 8,
                    blockLengthMinutes: 45
                )
            );
            db.SchoolYears.Add(year2025);
            await db.SaveChangesAsync(ct);
        }

        // 2) Subjects (all)
        var subjectNames = new[]
        {
            "Mathematics","Language Arts","English","Natural Sciences","Social Studies",
            "Physics","Chemistry","Biology","Technology","Computer Science",
            "Arts","Music","Physical Education","Religion","Ethics & Citizenship"
        };

        var subjects = await EnsureSubjectsAsync(db, subjectNames, ct);

        // 3) Grades (1..11)
        var grades = await EnsureGradesAsync(db, schoolYearId: year2025.Id, names: Enumerable.Range(1, 11).Select(n => n.ToString()), ct);

        // 4) Groups (only 6A, 6B)
        var grade6 = grades.Single(g => g.Name == "6");
        var group6A = await EnsureGroupAsync(db, grade6, "A", size: 30, ct);
        var group6B = await EnsureGroupAsync(db, grade6, "B", size: 28, ct);

        // 5) Study Plan (IH) for all Grades × Subjects — MVP defaults
        //    Adjust these hours to match the 2011 plan when you load it.
        var defaultHours = DefaultHoursBySubject();
        await EnsureStudyPlanAsync(db, year2025.Id, grades, subjects, defaultHours, ct);

        // 6) Teachers
        var math = subjects["Mathematics"].Id;
        var lang = subjects["Language Arts"].Id;
        var engl = subjects["English"].Id;
        var sci = subjects["Natural Sciences"].Id;
        var soc = subjects["Social Studies"].Id;
        var pe = subjects["Physical Education"].Id;
        var arts = subjects["Arts"].Id;
        var tech = subjects["Technology"].Id;

        var teachers = await EnsureTeachersAsync(db, new[]
        {
            (Name: "Ana García",      Subjects: new[]{ "Mathematics" }),
            (Name: "Carlos Mendoza",  Subjects: new[]{ "Language Arts", "Social Studies" }),
            (Name: "Laura Smith",     Subjects: new[]{ "English" }),
            (Name: "Julián Ríos",     Subjects: new[]{ "Natural Sciences", "Technology" }),
            (Name: "Paula Gómez",     Subjects: new[]{ "Arts" }),
            (Name: "Diego Pérez",     Subjects: new[]{ "Physical Education" }),
        }, ct);

        // 7) Assignments for 6A and 6B (respecting IH)
        await EnsureAssignmentsForGroupAsync(db, year2025.Id, group6A, subjects, teachers, defaultHours, ct);
        await EnsureAssignmentsForGroupAsync(db, year2025.Id, group6B, subjects, teachers, defaultHours, ct);

        await db.SaveChangesAsync(ct);
    }

    // ----------------------
    // Helpers
    // ----------------------

    private static async Task<Dictionary<string, Subject>> EnsureSubjectsAsync(StgDbContext db, IEnumerable<string> names, CancellationToken ct)
    {
        var existing = await db.Subjects.ToListAsync(ct);
        var map = existing.ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);

        foreach (var n in names)
        {
            if (!map.ContainsKey(n))
            {
                var subj = new Subject(n,
                    requiresLab: n is "Physics" or "Chemistry" or "Biology",
                    requiresDoublePeriod: n is "Physics" or "Chemistry" or "Biology");
                db.Subjects.Add(subj);
                map[n] = subj;
            }
        }
        await db.SaveChangesAsync(ct);
        return map;
    }

    private static async Task<List<Grade>> EnsureGradesAsync(StgDbContext db, Guid schoolYearId, IEnumerable<string> names, CancellationToken ct)
    {
        var result = new List<Grade>();
        foreach (var (name, order) in names.Select((n, i) => (n, (byte)(i + 1))))
        {
            var g = await db.Grades.FirstOrDefaultAsync(x => x.SchoolYearId == schoolYearId && x.Name == name, ct);
            if (g is null)
            {
                g = new Grade(schoolYearId, name, order);
                db.Grades.Add(g);
                await db.SaveChangesAsync(ct);
            }
            result.Add(g);
        }
        return result;
    }

    private static async Task<Group> EnsureGroupAsync(StgDbContext db, Grade grade, string label, ushort size, CancellationToken ct)
    {
        var g = await db.Groups.FirstOrDefaultAsync(x => x.GradeId == grade.Id && x.Label == label, ct);
        if (g is null)
        {
            g = new Group(grade.Id, grade.Name, label, size);
            db.Groups.Add(g);
            await db.SaveChangesAsync(ct);
        }
        return g;
    }

    private static Dictionary<string, byte> DefaultHoursBySubject() => new(StringComparer.OrdinalIgnoreCase)
    {
        // Reasonable MVP defaults, adjust with the official plan later
        ["Mathematics"] = 5,
        ["Language Arts"] = 5,
        ["English"] = 3,
        ["Natural Sciences"] = 3,
        ["Social Studies"] = 3,
        ["Physics"] = 2,
        ["Chemistry"] = 2,
        ["Biology"] = 2,
        ["Technology"] = 2,
        ["Computer Science"] = 2,
        ["Arts"] = 2,
        ["Music"] = 1,
        ["Physical Education"] = 2,
        ["Religion"] = 1,
        ["Ethics & Citizenship"] = 1,
    };

    private static async Task EnsureStudyPlanAsync(
        StgDbContext db,
        Guid schoolYearId,
        IEnumerable<Grade> grades,
        Dictionary<string, Subject> subjects,
        Dictionary<string, byte> defaultHours,
        CancellationToken ct)
    {
        foreach (var grade in grades)
        {
            foreach (var kvp in defaultHours)
            {
                // Use defaults for every grade. Replace later with per-grade hours if needed.
                var subj = subjects[kvp.Key];
                var hours = kvp.Value;

                var exists = await db.StudyPlanEntries
                    .AnyAsync(x => x.SchoolYearId == schoolYearId && x.GradeId == grade.Id && x.SubjectId == subj.Id, ct);

                if (!exists)
                {
                    db.StudyPlanEntries.Add(new StudyPlanEntry(schoolYearId, grade.Id, subj.Id, hours));
                }
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<string, Teacher>> EnsureTeachersAsync(
        StgDbContext db,
        IEnumerable<(string Name, IEnumerable<string> Subjects)> teachers,
        CancellationToken ct)
    {
        var map = new Dictionary<string, Teacher>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, subjects) in teachers)
        {
            var t = await db.Teachers.FirstOrDefaultAsync(x => x.Name == name, ct);
            if (t is null)
            {
                t = new Teacher(name, subjects);
                db.Teachers.Add(t);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                foreach (var s in subjects) t.AddSubject(s);
                await db.SaveChangesAsync(ct);
            }
            map[name] = t;
        }
        return map;
    }

    private static async Task EnsureAssignmentsForGroupAsync(
        StgDbContext db,
        Guid schoolYearId,
        Group group,
        Dictionary<string, Subject> subjects,
        Dictionary<string, Teacher> teachers,
        Dictionary<string, byte> defaultHours,
        CancellationToken ct)
    {
        // Simple assignment plan for demo: map subjects to teachers by specialty
        var mapping = new (string Subject, string Teacher)[]
        {
            ("Mathematics",        "Ana García"),
            ("Language Arts",      "Carlos Mendoza"),
            ("English",            "Laura Smith"),
            ("Natural Sciences",   "Julián Ríos"),
            ("Social Studies",     "Carlos Mendoza"),
            ("Technology",         "Julián Ríos"),
            ("Arts",               "Paula Gómez"),
            ("Physical Education", "Diego Pérez"),
        };

        foreach (var (subjName, teacherName) in mapping)
        {
            if (!subjects.TryGetValue(subjName, out var s) || !teachers.TryGetValue(teacherName, out var t))
                continue; // skip if not present

            var ih = defaultHours.TryGetValue(subjName, out var hours) ? hours : (byte)2;

            var exists = await db.Assignments.AnyAsync(a =>
                a.SchoolYearId == schoolYearId &&
                a.GroupId == group.Id &&
                a.SubjectId == s.Id, ct);

            if (!exists)
            {
                db.Assignments.Add(new Assignment(
                    schoolYearId: schoolYearId,
                    groupId: group.Id,
                    subjectId: s.Id,
                    teacherId: t.Id,
                    weeklyHours: ih
                ));
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
