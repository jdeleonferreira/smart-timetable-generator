using Microsoft.Extensions.DependencyInjection;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence;

/// <summary>
/// Seeds essential master data and a baseline StudyPlan (no Assignments).
/// Safe to run multiple times (idempotent checks).
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var years = sp.GetRequiredService<ISchoolYearRepository>();
        var grades = sp.GetRequiredService<IGradeRepository>();
        var groups = sp.GetRequiredService<IGroupRepository>();
        var areas = sp.GetRequiredService<IStudyAreaRepository>();
        var subjects = sp.GetRequiredService<ISubjectRepository>();
        var plans = sp.GetRequiredService<IStudyPlanRepository>();
        var teachers = sp.GetRequiredService<ITeacherRepository>();

        // 1) SchoolYear (current)
        var yearNumber = DateTime.UtcNow.Year;
        var sy = await years.GetByYearAsync(yearNumber, ct);
        if (sy is null)
        {
            sy = new SchoolYear(yearNumber);
            await years.AddAsync(sy, ct);
        }

        // 2) Grades (Preschool + 1..11)
        var gradeDefs = new (byte order, string name)[]
        {
            (0, "Preschool"),
            (1, "1st"), (2, "2nd"), (3, "3rd"), (4, "4th"), (5, "5th"),
            (6, "6th"), (7, "7th"), (8, "8th"), (9, "9th"), (10, "10th"), (11, "11th")
        };
        var gradeMap = new Dictionary<byte, Grade>();
        foreach (var (order, name) in gradeDefs)
        {
            var g = await grades.GetByOrderAsync(order, ct);
            if (g is null)
            {
                g = new Grade(Guid.NewGuid(), name, order);
                await grades.AddAsync(g, ct);
            }
            gradeMap[order] = g;
        }

        // 3) Groups (A, B) per grade
        foreach (var g in gradeMap.Values)
        {
            var existing = await groups.ListByGradeAsync(g.Id, ct);
            var set = existing.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toCreate = new List<Group>();
            foreach (var name in new[] { "A", "B" })
            {
                if (!set.Contains(name))
                    toCreate.Add(new Group(Guid.NewGuid(), g.Id, name));
            }
            if (toCreate.Count > 0)
                await groups.AddRangeAsync(toCreate, ct);
        }

        // 4) StudyAreas
        var areaDefs = new (string name, string? code, byte order)[]
        {
            ("Language Arts", "LANG", 1),
            ("Mathematics",    "MATH", 2),
            ("Science",        "SCI",  3),
            ("Social Studies", "SOC",  4),
            ("Foreign Language","FL",  5),
            ("Arts",           "ART",  6),
            ("Physical Education","PE",7),
            ("Technology",     "TECH", 8),
        };
        var areaMap = new Dictionary<string, StudyArea>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, code, orderNo) in areaDefs)
        {
            var a = await areas.GetByNameAsync(name, ct);
            if (a is null)
            {
                a = new StudyArea(Guid.NewGuid(), name, code, orderNo, isActive: true);
                await areas.AddAsync(a, ct);
            }
            areaMap[name] = a;
        }

        // 5) Subjects (per area)
        var subjectDefs = new (string areaName, string subjectName, string? code, bool elective)[]
        {
            ("Language Arts", "Reading & Writing", "LANG_RW", false),
            ("Language Arts", "Literature",        "LANG_LIT", false),

            ("Mathematics",   "Mathematics",       "MATH_GEN", false),
            ("Mathematics",   "Algebra",           "MATH_ALG", false),

            ("Science",       "General Science",   "SCI_GEN",  false),
            ("Science",       "Biology",           "SCI_BIO",  false),

            ("Social Studies","Civics",            "SOC_CIV",  false),
            ("Social Studies","History",           "SOC_HIS",  false),

            ("Foreign Language","English",         "FL_ENG",   false),
            ("Foreign Language","French",          "FL_FR",    true),

            ("Arts",          "Visual Arts",       "ART_VA",   true),
            ("Physical Education","Physical Education","PE_GEN", false),

            ("Technology",    "Computing",         "TECH_IT",  true),
        };
        var subjectMap = new Dictionary<string, Subject>(StringComparer.OrdinalIgnoreCase);
        foreach (var (areaName, subjectName, code, elective) in subjectDefs)
        {
            var s = await subjects.GetByNameAsync(subjectName, ct);
            if (s is null)
            {
                var area = areaMap[areaName];
                s = new Subject(Guid.NewGuid(), subjectName, area.Id, code, elective);
                await subjects.AddAsync(s, ct);
            }
            subjectMap[subjectName] = s;
        }

        // 6) Teachers (sample)
        var teacherDefs = new (string fullName, string? email, byte? cap)[]
        {
            ("Alice Johnson",  "alice@school.edu", 30),
            ("Bob Smith",      "bob@school.edu",   28),
            ("Carla Gomez",    "carla@school.edu", null),
            ("Daniel Lee",     "daniel@school.edu",26),
            ("Eva Martinez",   "eva@school.edu",   30),
            ("Frank Zhang",    null,               null),
        };
        foreach (var (fullName, email, cap) in teacherDefs)
        {
            var exists = !string.IsNullOrWhiteSpace(email)
                ? await teachers.GetByEmailAsync(email!, ct)
                : null;

            if (exists is null)
            {
                var t = new Teacher(Guid.NewGuid(), fullName, email, cap, isActive: true);
                await teachers.AddAsync(t, ct);
            }
        }

        // 7) StudyPlan (one per SchoolYear) + Entries (base hours)
        var plan = await plans.GetByYearAsync(yearNumber, ct);
        if (plan is null)
        {
            plan = new StudyPlan(Guid.NewGuid(), sy.Id, $"Plan {yearNumber}", "Default seeded plan");
            await plans.AddAsync(plan, ct);
        }

        // Base hours (adjust as desired)
        var baseHours = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mathematics"] = 4,
            ["Reading & Writing"] = 4,
            ["General Science"] = 3,
            ["Civics"] = 3,
            ["English"] = 3,
            ["Physical Education"] = 2,
            ["Visual Arts"] = 2,
            ["Computing"] = 1,
        };

        foreach (var grade in gradeMap.Values.Where(g => g.Order >= 1 && g.Order <= 11))
        {
            // Use IStudyPlanRepository.UpsertEntryAsync (✅ correct place)
            foreach (var kv in baseHours)
            {
                var subjId = subjectMap[kv.Key].Id;
                await plans.UpsertEntryAsync(plan.Id, grade.Id, subjId, kv.Value, notes: null, ct);
            }

            if (grade.Order >= 7)
            {
                await plans.UpsertEntryAsync(plan.Id, grade.Id, subjectMap["Biology"].Id, 2, null, ct);
                await plans.UpsertEntryAsync(plan.Id, grade.Id, subjectMap["Algebra"].Id, 2, null, ct);
            }
        }

    }
}
