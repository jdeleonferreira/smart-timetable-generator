using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.Timetables;

namespace SmartTimetableGenerator.Domain.UnitTests.Timetables;

public class TimetableTests
{
    private static readonly ShiftId Morning = ShiftId.From(Guid.NewGuid());
    private static readonly CourseId CourseA = CourseId.From(Guid.NewGuid());
    private static readonly CourseId CourseB = CourseId.From(Guid.NewGuid());
    private static readonly SubjectId English = SubjectId.From(Guid.NewGuid());
    private static readonly TeacherId MainTeacher = TeacherId.From(Guid.NewGuid());
    private static readonly SpaceId Lab = SpaceId.From(Guid.NewGuid());

    private static Timetable NewTimetable() => Timetable.Create(
        AcademicYearId.From(Guid.NewGuid()),
        CampusId.From(Guid.NewGuid()),
        AcademicPeriodId.From(Guid.NewGuid()),
        StudyPlanId.From(Guid.NewGuid()),
        "Horario");

    private static LessonPlacement At(CourseId course, TeacherId? teacher, SpaceId? space, DayOfWeek day, int period) =>
        new(course, English, teacher, space, Morning, day, period);

    [Fact]
    public void SameTeacherSameSlot_ShouldConflict()
    {
        var timetable = NewTimetable();
        timetable.AddLesson(At(CourseA, MainTeacher, null, DayOfWeek.Monday, 1));

        var result = timetable.AddLesson(At(CourseB, MainTeacher, null, DayOfWeek.Monday, 1));

        result.FirstError.Should().Be(TimetableErrors.TeacherBusy);
    }

    [Fact]
    public void SameSpaceSameSlot_ShouldConflict()
    {
        var timetable = NewTimetable();
        timetable.AddLesson(At(CourseA, null, Lab, DayOfWeek.Monday, 1));

        var result = timetable.AddLesson(At(CourseB, null, Lab, DayOfWeek.Monday, 1));

        result.FirstError.Should().Be(TimetableErrors.SpaceBusy);
    }

    [Fact]
    public void SameCourseSameSlot_ShouldConflict()
    {
        var timetable = NewTimetable();
        timetable.AddLesson(At(CourseA, null, null, DayOfWeek.Monday, 1));

        var result = timetable.AddLesson(At(CourseA, null, null, DayOfWeek.Monday, 1));

        result.FirstError.Should().Be(TimetableErrors.CourseBusy);
    }

    [Fact]
    public void ReplaceUnlockedLessons_ShouldKeepLockedLessons()
    {
        var timetable = NewTimetable();
        timetable.AddLesson(At(CourseA, MainTeacher, null, DayOfWeek.Wednesday, 3), isLocked: true);
        timetable.AddLesson(At(CourseB, null, null, DayOfWeek.Monday, 1));

        var result = timetable.ReplaceUnlockedLessons([At(CourseB, null, null, DayOfWeek.Tuesday, 2)]);

        result.IsError.Should().BeFalse();
        timetable.Lessons.Should().HaveCount(2);
        timetable.Lessons.Should().ContainSingle(l => l.IsLocked && l.Day == DayOfWeek.Wednesday);
        timetable.Lessons.Should().ContainSingle(l => l.CourseId == CourseB && l.Day == DayOfWeek.Tuesday);
    }

    [Fact]
    public void ReplaceUnlockedLessons_WithConflict_ShouldKeepPreviousLessons()
    {
        var timetable = NewTimetable();
        timetable.AddLesson(At(CourseA, MainTeacher, null, DayOfWeek.Wednesday, 3), isLocked: true);
        timetable.AddLesson(At(CourseB, null, null, DayOfWeek.Monday, 1));

        var result = timetable.ReplaceUnlockedLessons([At(CourseB, MainTeacher, null, DayOfWeek.Wednesday, 3)]);

        result.FirstError.Should().Be(TimetableErrors.TeacherBusy);
        timetable.Lessons.Should().HaveCount(2);
    }

    [Fact]
    public void PublishedTimetable_ShouldNotBeEditable()
    {
        var timetable = NewTimetable();
        timetable.AddLesson(At(CourseA, null, null, DayOfWeek.Monday, 1));
        timetable.Publish(DateTimeOffset.UtcNow);

        var result = timetable.AddLesson(At(CourseA, null, null, DayOfWeek.Monday, 2));

        result.FirstError.Should().Be(TimetableErrors.NotEditable);
    }
}
