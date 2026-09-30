using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Domain.UnitTests.StudyPlans;

public class StudyPlanTests
{
    private static readonly GradeId SixthGrade = GradeId.From(Guid.NewGuid());
    private static readonly SubjectId MathSubject = SubjectId.From(Guid.NewGuid());
    private static readonly SubjectId Social = SubjectId.From(Guid.NewGuid());
    private static readonly SubjectId Citizenship = SubjectId.From(Guid.NewGuid());
    private static readonly SubjectId Research = SubjectId.From(Guid.NewGuid());

    private static StudyPlan NewPlan() =>
        StudyPlan.Create(AcademicYearId.From(Guid.NewGuid()), CampusId.From(Guid.NewGuid()), "Plan");

    [Fact]
    public void WeeklyTotal_ShouldCountOnlyRegularSubjects()
    {
        var plan = NewPlan();
        plan.AddItem(SixthGrade, MathSubject, DeliveryMode.Regular, 5);
        plan.AddItem(SixthGrade, Social, DeliveryMode.Regular, 4);
        plan.AddItem(SixthGrade, Citizenship, DeliveryMode.Transversal, 0, integratedIntoSubjectId: Social);
        plan.AddItem(SixthGrade, Research, DeliveryMode.CounterShift, 2, targetShiftId: ShiftId.From(Guid.NewGuid()));

        plan.WeeklyTotal(SixthGrade).Should().Be(9);
        plan.WeeklyCounterShiftTotal(SixthGrade).Should().Be(2);
    }

    [Fact]
    public void AddItem_Twice_ShouldFail()
    {
        var plan = NewPlan();
        plan.AddItem(SixthGrade, MathSubject, DeliveryMode.Regular, 5);

        var result = plan.AddItem(SixthGrade, MathSubject, DeliveryMode.Regular, 3);

        result.FirstError.Should().Be(StudyPlanErrors.DuplicateItem);
    }

    [Fact]
    public void CounterShift_WithoutShift_ShouldFail()
    {
        var result = NewPlan().AddItem(SixthGrade, Research, DeliveryMode.CounterShift, 2);

        result.FirstError.Should().Be(StudyPlanErrors.CounterShiftRequiresShift);
    }

    [Fact]
    public void Transversal_ShouldHaveNoHours()
    {
        var plan = NewPlan();

        var item = plan.AddItem(SixthGrade, Citizenship, DeliveryMode.Transversal, 3).Value;

        item.WeeklyHours.Should().Be(0);
        item.HoursFor(null).Should().Be(0);
    }

    [Fact]
    public void PeriodHours_ShouldOverrideGeneralHours()
    {
        var plan = NewPlan();
        var item = plan.AddItem(SixthGrade, MathSubject, DeliveryMode.Regular, 5).Value;
        var period = AcademicPeriodId.From(Guid.NewGuid());

        plan.SetItemPeriodHours(item.Id, period, 3);

        item.HoursFor(period).Should().Be(3);
        item.HoursFor(AcademicPeriodId.From(Guid.NewGuid())).Should().Be(5);
        plan.WeeklyTotal(SixthGrade, period).Should().Be(3);
    }

    [Fact]
    public void ApprovedPlan_ShouldNotBeEditable()
    {
        var plan = NewPlan();
        plan.AddItem(SixthGrade, MathSubject, DeliveryMode.Regular, 5);
        plan.Approve(DateTimeOffset.UtcNow);

        var result = plan.AddItem(SixthGrade, Social, DeliveryMode.Regular, 4);

        result.FirstError.Should().Be(StudyPlanErrors.NotEditable);
    }

    [Fact]
    public void CreateCopy_ForAnotherYear_ShouldCopyItemsWithoutPeriodHours()
    {
        var plan = NewPlan();
        var item = plan.AddItem(SixthGrade, MathSubject, DeliveryMode.Regular, 5).Value;
        plan.SetItemPeriodHours(item.Id, AcademicPeriodId.From(Guid.NewGuid()), 3);

        var copy = StudyPlan.CreateCopy(plan, AcademicYearId.From(Guid.NewGuid()), plan.CampusId, "Plan 2027");

        copy.Items.Should().ContainSingle();
        copy.Items[0].Id.Should().NotBe(item.Id);
        copy.Items[0].WeeklyHours.Should().Be(5);
        copy.Items[0].PeriodHours.Should().BeEmpty();
        copy.Status.Should().Be(StudyPlanStatus.Draft);
    }
}
