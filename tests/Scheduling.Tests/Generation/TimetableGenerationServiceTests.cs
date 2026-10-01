using Microsoft.EntityFrameworkCore;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.TeachingAssignments;
using SmartTimetableGenerator.Domain.TimetableGeneration;
using SmartTimetableGenerator.Domain.Timetables;
using SmartTimetableGenerator.Scheduling.Tests.Common;

namespace SmartTimetableGenerator.Scheduling.Tests.Generation;

/// <summary>
/// Servicio de generación completo (EF Core + SQLite, motor CP-SAT real) sobre el colegio de prueba.
/// Cada horario resultante se revisa con <see cref="TimetableValidator"/> además de las comprobaciones propias de la prueba.
/// </summary>
public sealed class TimetableGenerationServiceTests : IDisposable
{
    private const int TimeLimitSeconds = 30;

    private readonly SchedulingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private sealed record Outcome(GenerationJob Job, Timetable Timetable, List<TeachingAssignment> Assignments);

    private async Task<Outcome> GenerateAsync(TestSchool school, Action<IApplicationDbContext>? extraSeed = null)
    {
        await _host.SeedAsync(db =>
        {
            school.Seed(db);
            extraSeed?.Invoke(db);
        });

        return await RegenerateAsync(school);
    }

    private async Task<Outcome> RegenerateAsync(TestSchool school)
    {
        var job = GenerationJob.Create(school.Timetable.Id, DateTimeOffset.UtcNow, TimeLimitSeconds);
        await _host.SeedAsync(db => db.GenerationJobs.Add(job));
        await _host.RunJobAsync(job.Id);
        return await LoadAsync(school, job.Id);
    }

    private Task<Outcome> LoadAsync(TestSchool school, GenerationJobId jobId) =>
        _host.WithDbAsync(async db => new Outcome(
            await db.GenerationJobs.FirstAsync(j => j.Id == jobId),
            await db.Timetables.Include(t => t.Lessons).FirstAsync(t => t.Id == school.Timetable.Id),
            await db.TeachingAssignments.ToListAsync()));

    private static int TotalHours(TestSchool school) =>
        school.Courses.Sum(c => school.Plan.ItemsForGrade(c.GradeId)
            .Where(i => i.DeliveryMode != DeliveryMode.Transversal)
            .Sum(i => i.HoursFor(school.Period1.Id)));

    [Fact]
    public async Task Generate_ShouldProduceACompleteAndStrictlyValidTimetable()
    {
        var school = new TestSchool();

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        result.Job.PlacedLessons.Should().Be(4 * TestSchool.BaseWeeklyHours);
        result.Job.UnplacedLessons.Should().Be(0);
        result.Job.StartedAt.Should().NotBeNull();
        result.Job.FinishedAt.Should().NotBeNull();
        result.Timetable.Lessons.Should().HaveCount(4 * TestSchool.BaseWeeklyHours);
        result.Timetable.Lessons.Should().OnlyContain(l => l.TeacherId != null && !l.IsLocked);
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task Generate_ShouldLeaveNoGapsAndStartEveryDayAtTheFirstPeriod()
    {
        var school = new TestSchool();

        var result = await GenerateAsync(school);

        foreach (var day in result.Timetable.Lessons.GroupBy(l => (l.CourseId, l.Day)))
        {
            var periods = day.Select(l => l.PeriodNumber).OrderBy(p => p).ToList();
            periods[0].Should().Be(1, $"el curso debe empezar a primera hora el {day.Key.Day}");
            periods.Should().Equal(Enumerable.Range(1, periods.Count), $"no debe haber huecos el {day.Key.Day}");
        }
    }

    [Fact]
    public async Task Generate_ShouldSaveOneSuggestedAssignmentPerCourseAndSubject()
    {
        var school = new TestSchool();

        var result = await GenerateAsync(school);

        result.Assignments.Should().HaveCount(4 * 6);
        result.Assignments.Should().OnlyContain(a => a.Source == AssignmentSource.Suggested && a.TeacherId != null);
        result.Assignments.GroupBy(a => (a.CourseId, a.SubjectId)).Should().OnlyContain(g => g.Count() == 1);
    }

    [Fact]
    public async Task Generate_ShouldGiveTheSameTeacherToAllCoursesOfAGradeWhenCapacityAllows()
    {
        var school = new TestSchool();

        var result = await GenerateAsync(school);

        foreach (var grade in new[] { school.Sixth, school.Seventh })
        {
            var courseIds = school.CoursesOf(grade).Select(c => c.Id).ToHashSet();
            foreach (var bySubject in result.Assignments.Where(a => courseIds.Contains(a.CourseId)).GroupBy(a => a.SubjectId))
                bySubject.Select(a => a.TeacherId).Distinct().Should().ContainSingle();
        }
    }

    [Fact]
    public async Task Generate_ShouldNeverExceedTeacherWeeklyLoad()
    {
        var school = new TestSchool();

        var result = await GenerateAsync(school);

        result.Timetable.Lessons.GroupBy(l => l.TeacherId).Should().OnlyContain(g => g.Count() <= 22);
    }

    [Fact]
    public async Task ManualAssignment_ShouldBeRespectedAndStayManual()
    {
        var school = new TestSchool();
        var manual = TeachingAssignment.Create(school.Year.Id, school.C6A.Id, school.English.Id);
        manual.AssignManually(school.HumanitiesTeacher2.Id);

        var result = await GenerateAsync(school, db => db.TeachingAssignments.Add(manual));

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        var english6A = result.Timetable.Lessons.Where(l => l.CourseId == school.C6A.Id && l.SubjectId == school.English.Id).ToList();
        english6A.Should().HaveCount(4);
        english6A.Should().OnlyContain(l => l.TeacherId == school.HumanitiesTeacher2.Id);
        var saved = result.Assignments.Single(a => a.CourseId == school.C6A.Id && a.SubjectId == school.English.Id);
        saved.Source.Should().Be(AssignmentSource.Manual);
        saved.TeacherId.Should().Be(school.HumanitiesTeacher2.Id);
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task ManualAssignmentOfATeacherWithoutTheArea_ShouldStillBeRespected()
    {
        var school = new TestSchool();
        var manual = TeachingAssignment.Create(school.Year.Id, school.C7B.Id, school.Informatics.Id);
        manual.AssignManually(school.MathTeacher.Id);

        var result = await GenerateAsync(school, db => db.TeachingAssignments.Add(manual));

        result.Timetable.Lessons
            .Where(l => l.CourseId == school.C7B.Id && l.SubjectId == school.Informatics.Id)
            .Should().OnlyContain(l => l.TeacherId == school.MathTeacher.Id);
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task LockedLesson_ShouldStayInPlace_AndCountTowardTheWeeklyHours()
    {
        var school = new TestSchool();
        school.Timetable.AddLesson(new LessonPlacement(school.C6A.Id, school.Arithmetic.Id, school.MathTeacher.Id,
            school.C6A.HomeRoomId, school.Morning.Id, DayOfWeek.Friday, 6), isLocked: true);

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        result.Timetable.Lessons.Should().ContainSingle(l => l.IsLocked);
        result.Timetable.Lessons.Should().ContainSingle(l => l.IsLocked && l.CourseId == school.C6A.Id &&
            l.SubjectId == school.Arithmetic.Id && l.Day == DayOfWeek.Friday && l.PeriodNumber == 6);
        result.Timetable.Lessons.Count(l => l.CourseId == school.C6A.Id && l.SubjectId == school.Arithmetic.Id).Should().Be(5);
        result.Timetable.Lessons.Should().HaveCount(4 * TestSchool.BaseWeeklyHours);
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task TeacherUnavailability_ShouldNeverBeViolated()
    {
        var school = new TestSchool();
        school.MathTeacher.SetAvailability(
        [
            new AvailabilityRule(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(9, 30), AvailabilityKind.Unavailable),
            new AvailabilityRule(DayOfWeek.Thursday, new TimeOnly(10, 0), new TimeOnly(12, 30), AvailabilityKind.Unavailable)
        ]).IsError.Should().BeFalse();

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        result.Timetable.Lessons.Where(l => l.TeacherId == school.MathTeacher.Id)
            .Should().OnlyContain(l => !(l.Day == DayOfWeek.Monday && l.PeriodNumber <= 3) &&
                                       !(l.Day == DayOfWeek.Thursday && l.PeriodNumber >= 4));
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task TeacherBusyInAnotherCampus_ShouldNotOverlapInTime_AndCountTowardHisLoad()
    {
        var school = new TestSchool();
        school.ScienceTeacher.SetCampuses([school.Campus.Id, school.OtherCampus.Id]);
        var otherPlan = StudyPlan.Create(school.Year.Id, school.OtherCampus.Id, "Plan sede norte");
        var other = Timetable.Create(school.Year.Id, school.OtherCampus.Id, school.Period1.Id, otherPlan.Id, "Sede norte");
        var otherCourse = SmartTimetableGenerator.Domain.Courses.Course.Create(school.Year.Id, school.OtherCampus.Id, school.OtherMorning.Id, school.Sixth.Id, "N");
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            // 07:00–07:50 todos los días en la otra sede
            other.AddLesson(new LessonPlacement(otherCourse.Id, school.Biology.Id, school.ScienceTeacher.Id, null, school.OtherMorning.Id, day, 1));
        }

        var result = await GenerateAsync(school, db =>
        {
            db.StudyPlans.Add(otherPlan);
            db.Courses.Add(otherCourse);
            db.Timetables.Add(other);
        });

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        result.Timetable.Lessons.Where(l => l.TeacherId == school.ScienceTeacher.Id).Should().OnlyContain(l => l.PeriodNumber != 1);
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments, other.Lessons);
    }

    [Fact]
    public async Task RequiredSpaceType_ShouldUseASpaceOfThatType()
    {
        var school = new TestSchool();
        foreach (var grade in new[] { school.Sixth, school.Seventh })
            school.Plan.SetItemDistribution(school.Item(grade, school.Informatics).Id, 1, 1, SpaceType.ComputerLab);

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        var informatics = result.Timetable.Lessons.Where(l => l.SubjectId == school.Informatics.Id).ToList();
        informatics.Should().HaveCount(8);
        informatics.Should().OnlyContain(l => l.SpaceId == school.ComputerLab.Id);
        informatics.GroupBy(l => (l.Day, l.PeriodNumber)).Should().OnlyContain(g => g.Count() == 1);
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task RequiredSpaceTypeWithoutSuchSpace_ShouldUseTheHomeRoomAndReportIt()
    {
        var school = new TestSchool();
        school.Plan.SetItemDistribution(school.Item(school.Sixth, school.Biology).Id, 2, 2, SpaceType.Laboratory);

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        result.Job.Message.Should().Contain("No hay espacio de tipo Laboratory");
        foreach (var course in school.CoursesOf(school.Sixth))
        {
            result.Timetable.Lessons.Where(l => l.CourseId == course.Id && l.SubjectId == school.Biology.Id)
                .Should().OnlyContain(l => l.SpaceId == course.HomeRoomId);
        }

        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task CounterShiftSubject_ShouldBeScheduledInTheTargetShift_WithoutTheHomeRoom()
    {
        var school = new TestSchool();
        school.Plan.AddItem(school.Seventh.Id, school.Research.Id, DeliveryMode.CounterShift, 2, targetShiftId: school.Afternoon.Id)
            .IsError.Should().BeFalse();

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        var research = result.Timetable.Lessons.Where(l => l.SubjectId == school.Research.Id).ToList();
        research.Should().HaveCount(4);
        research.Should().OnlyContain(l => l.ShiftId == school.Afternoon.Id && l.SpaceId == null && l.TeacherId == school.ScienceTeacher.Id);
        research.Select(l => l.CourseId).Distinct().Should().HaveCount(2);
        result.Timetable.Lessons.Where(l => l.ShiftId == school.Morning.Id).Should().HaveCount(4 * TestSchool.BaseWeeklyHours);
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task TeacherDailyMax_ShouldHoldAcrossMorningAndAfternoon()
    {
        var school = new TestSchool();
        // 16 h de Biología en la mañana + 4 h de Investigación en la tarde = 20 h con máximo 4 por día: exactamente 4 cada día
        school.ScienceTeacher.SetWorkload(22, 4, null).IsError.Should().BeFalse();
        school.Plan.AddItem(school.Seventh.Id, school.Research.Id, DeliveryMode.CounterShift, 2, targetShiftId: school.Afternoon.Id)
            .IsError.Should().BeFalse();

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        var science = result.Timetable.Lessons.Where(l => l.TeacherId == school.ScienceTeacher.Id).ToList();
        science.Should().HaveCount(20);
        science.GroupBy(l => l.Day).Should().HaveCount(5);
        science.GroupBy(l => l.Day).Should().OnlyContain(g => g.Count() == 4);
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task TransversalSubject_ShouldProduceNoLessons()
    {
        var school = new TestSchool();
        school.Plan.AddItem(school.Sixth.Id, school.Citizenship.Id, DeliveryMode.Transversal, 0, integratedIntoSubjectId: school.Arithmetic.Id)
            .IsError.Should().BeFalse();

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        result.Timetable.Lessons.Should().OnlyContain(l => l.SubjectId != school.Citizenship.Id);
        result.Assignments.Should().OnlyContain(a => a.SubjectId != school.Citizenship.Id);
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task PeriodHours_ShouldBeUsedForTheTimetablePeriod()
    {
        var school = new TestSchool();
        school.Plan.SetItemPeriodHours(school.Item(school.Sixth, school.Arithmetic).Id, school.Period1.Id, 3).IsError.Should().BeFalse();
        school.Plan.SetItemPeriodHours(school.Item(school.Seventh, school.Arithmetic).Id, school.Period2.Id, 1).IsError.Should().BeFalse();

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        foreach (var course in school.CoursesOf(school.Sixth))
            result.Timetable.Lessons.Count(l => l.CourseId == course.Id && l.SubjectId == school.Arithmetic.Id).Should().Be(3);
        foreach (var course in school.CoursesOf(school.Seventh))
            result.Timetable.Lessons.Count(l => l.CourseId == course.Id && l.SubjectId == school.Arithmetic.Id).Should().Be(5);
        result.Timetable.Lessons.Should().HaveCount(TotalHours(school));
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task NoTeacherForAnArea_ShouldScheduleWithoutTeacher_AndReportIt()
    {
        var school = new TestSchool();
        school.SportsTeacher.Deactivate();

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        result.Job.Message.Should().Contain("Sin docente disponible");
        var pe = result.Timetable.Lessons.Where(l => l.SubjectId == school.PhysicalEducation.Id).ToList();
        pe.Should().HaveCount(8);
        pe.Should().OnlyContain(l => l.TeacherId == null);
        result.Timetable.Lessons.Where(l => l.SubjectId != school.PhysicalEducation.Id).Should().OnlyContain(l => l.TeacherId != null);
        result.Timetable.Lessons.Should().HaveCount(4 * TestSchool.BaseWeeklyHours);
    }

    [Fact]
    public async Task NoTeachersAtAll_ShouldStillGenerateTheTimetable_SoTeachersCanBeAssignedLater()
    {
        var school = new TestSchool();
        foreach (var teacher in school.Teachers)
            teacher.Deactivate();

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        result.Timetable.Lessons.Should().HaveCount(4 * TestSchool.BaseWeeklyHours);
        result.Timetable.Lessons.Should().OnlyContain(l => l.TeacherId == null);
    }

    [Fact]
    public async Task CoursesSharingAClassroom_ShouldNotFailTheGeneration()
    {
        var school = new TestSchool();
        var sixth = school.CoursesOf(school.Sixth).ToList();
        sixth[1].AssignHomeRoom(sixth[0].HomeRoomId);

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        result.Timetable.Lessons.Should().HaveCount(4 * TestSchool.BaseWeeklyHours);
        result.Timetable.Lessons
            .Where(l => l.SpaceId is not null)
            .GroupBy(l => (l.ShiftId, l.Day, l.PeriodNumber, l.SpaceId))
            .Should().OnlyContain(g => g.Count() == 1);
    }

    [Fact]
    public async Task ClassroomThatIsAlsoASpecialSpace_ShouldBeRespectedBySolver()
    {
        var school = new TestSchool();
        school.CoursesOf(school.Sixth).First().AssignHomeRoom(school.ComputerLab.Id);

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Succeeded, result.Job.Message);
        result.Timetable.Lessons
            .Where(l => l.SpaceId == school.ComputerLab.Id)
            .GroupBy(l => (l.ShiftId, l.Day, l.PeriodNumber))
            .Should().OnlyContain(g => g.Count() == 1);
    }

    [Fact]
    public async Task TeacherWithoutEnoughHours_ShouldNotBeOverloaded()
    {
        var school = new TestSchool();
        // El único docente de Matemáticas solo puede 12 h: 20 h de Aritmética no le caben
        school.MathTeacher.SetWorkload(12, 6, null).IsError.Should().BeFalse();

        var result = await GenerateAsync(school);

        result.Timetable.Lessons.Count(l => l.TeacherId == school.MathTeacher.Id).Should().BeLessThanOrEqualTo(12);
        result.Job.Message.Should().Contain("Sin docente disponible");
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments);
    }

    [Fact]
    public async Task MoreHoursThanFit_ShouldPartiallySucceed_WithTheExactUnplacedCount()
    {
        var school = new TestSchool();
        // Educación física de 6º pasa de 2 a 12 h (máximo 2 por día → caben 10): 6ºA y 6ºB quedan con 32 h en 30 franjas
        school.Plan.UpdateItem(school.Item(school.Sixth, school.PhysicalEducation).Id, DeliveryMode.Regular, 12).IsError.Should().BeFalse();
        school.SportsTeacher.SetWorkload(40, 6, null).IsError.Should().BeFalse();

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.PartiallySucceeded, result.Job.Message);
        result.Job.UnplacedLessons.Should().Be(4);
        result.Job.Message.Should().Contain("Sin ubicar");
        foreach (var course in school.CoursesOf(school.Sixth))
        {
            result.Timetable.Lessons.Count(l => l.CourseId == course.Id).Should().Be(30);
            result.Timetable.Lessons.Count(l => l.CourseId == course.Id && l.SubjectId == school.PhysicalEducation.Id).Should().Be(10);
        }

        result.Job.PlacedLessons.Should().Be(result.Timetable.Lessons.Count);
        result.Timetable.ShouldBeStrictlyValid(school, result.Assignments, expectComplete: false);
    }

    [Fact]
    public async Task ContradictoryLockedLessons_ShouldBeInfeasible_AndKeepThePreviousLessons()
    {
        var school = new TestSchool();
        // Informática admite 1 h por día, pero hay 2 clases fijadas el lunes
        school.Timetable.AddLesson(new LessonPlacement(school.C6A.Id, school.Informatics.Id, null, school.C6A.HomeRoomId, school.Morning.Id, DayOfWeek.Monday, 1), isLocked: true);
        school.Timetable.AddLesson(new LessonPlacement(school.C6A.Id, school.Informatics.Id, null, school.C6A.HomeRoomId, school.Morning.Id, DayOfWeek.Monday, 2), isLocked: true);

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Infeasible);
        result.Job.Message.Should().Contain("conservó");
        result.Timetable.Lessons.Should().HaveCount(2);
        result.Timetable.Lessons.Should().OnlyContain(l => l.IsLocked);
    }

    [Fact]
    public async Task PublishedTimetable_ShouldFailTheJob_AndNotTouchTheLessons()
    {
        var school = new TestSchool();
        school.Timetable.AddLesson(new LessonPlacement(school.C6A.Id, school.Arithmetic.Id, school.MathTeacher.Id, school.C6A.HomeRoomId, school.Morning.Id, DayOfWeek.Monday, 1));
        school.Timetable.Publish(DateTimeOffset.UtcNow).IsError.Should().BeFalse();

        var result = await GenerateAsync(school);

        result.Job.Status.Should().Be(GenerationJobStatus.Failed);
        result.Job.Message.Should().Contain("borrador");
        result.Timetable.Status.Should().Be(TimetableStatus.Published);
        result.Timetable.Lessons.Should().ContainSingle();
    }

    [Fact]
    public async Task JobThatIsNotQueued_ShouldBeIgnored()
    {
        var school = new TestSchool();
        var first = await GenerateAsync(school);
        var lessonIds = first.Timetable.Lessons.Select(l => l.Id).ToHashSet();

        await _host.RunJobAsync(first.Job.Id);
        var again = await LoadAsync(school, first.Job.Id);

        again.Job.Status.Should().Be(GenerationJobStatus.Succeeded);
        again.Job.FinishedAt.Should().Be(first.Job.FinishedAt);
        again.Timetable.Lessons.Select(l => l.Id).ToHashSet().SetEquals(lessonIds).Should().BeTrue();
    }

    [Fact]
    public async Task Regenerate_ShouldReplaceUnlockedLessons_AndKeepLockedOnes()
    {
        var school = new TestSchool();
        var first = await GenerateAsync(school);
        var toLock = first.Timetable.Lessons.First(l => l.CourseId == school.C7A.Id);

        await _host.WithDbAsync(async db =>
        {
            var timetable = await db.Timetables.Include(t => t.Lessons).FirstAsync(t => t.Id == school.Timetable.Id);
            return timetable.SetLessonLocked(toLock.Id, true);
        });

        var second = await RegenerateAsync(school);

        second.Job.Status.Should().Be(GenerationJobStatus.Succeeded, second.Job.Message);
        second.Timetable.Lessons.Should().HaveCount(4 * TestSchool.BaseWeeklyHours);
        second.Timetable.Lessons.Should().ContainSingle(l => l.IsLocked);
        var kept = second.Timetable.Lessons.Single(l => l.IsLocked);
        kept.Id.Should().Be(toLock.Id);
        (kept.Day, kept.PeriodNumber, kept.SubjectId, kept.TeacherId).Should().Be((toLock.Day, toLock.PeriodNumber, toLock.SubjectId, toLock.TeacherId));
        second.Timetable.Lessons.Where(l => !l.IsLocked).Select(l => l.Id).Should().OnlyContain(id => id != toLock.Id);
        second.Timetable.ShouldBeStrictlyValid(school, second.Assignments);
    }
}
