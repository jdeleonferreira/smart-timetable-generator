using ErrorOr;
using Microsoft.EntityFrameworkCore;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.AddStudyPlanItem;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.ApproveStudyPlan;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.CreateStudyPlan;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.RemoveStudyPlanItem;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.SetStudyPlanItemDistribution;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.SetStudyPlanItemPeriodHours;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.UpdateStudyPlan;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.UpdateStudyPlanItem;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Queries.GetStudyPlan;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Queries.GetStudyPlans;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.TimetableGeneration;
using SmartTimetableGenerator.Scheduling.Tests.Common;

namespace SmartTimetableGenerator.Scheduling.Tests.StudyPlans;

/// <summary>
/// Gestión del plan de estudios por MediatR (con validadores y EF Core): consulta en forma de matriz,
/// creación (vacío o copiando), líneas asignatura × grado, distribución, IH por periodo y aprobación.
/// Cada cambio se comprueba volviendo a leer el plan desde la base de datos.
/// </summary>
public sealed class StudyPlanCommandsTests : IDisposable
{
    private readonly SchedulingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private async Task<TestSchool> SeedAsync(Action<TestSchool>? adjust = null)
    {
        var school = new TestSchool();
        adjust?.Invoke(school);
        await _host.SeedAsync(school.Seed);
        return school;
    }

    private async Task<AcademicYear> SeedNextYearAsync()
    {
        var year = AcademicYear.Create(2027, new DateOnly(2027, 1, 25), new DateOnly(2027, 11, 26));
        year.AddPeriod("Periodo 1", new DateOnly(2027, 1, 25), new DateOnly(2027, 6, 18)).IsError.Should().BeFalse();
        await _host.SeedAsync(db => db.AcademicYears.Add(year));
        return year;
    }

    private async Task<StudyPlanDto> GetPlanAsync(Guid id) => (await _host.SendAsync(new GetStudyPlanQuery(id))).Value;

    private static StudyPlanItemDto ItemOf(StudyPlanDto plan, Subject subject, Grade grade) =>
        plan.Areas.SelectMany(a => a.Subjects).Single(s => s.Id == subject.Id.Value).Items.Single(i => i.GradeId == grade.Id.Value);

    private static StudyPlanGradeDto GradeOf(StudyPlanDto plan, Grade grade) => plan.Grades.Single(g => g.Id == grade.Id.Value);

    private Task<ErrorOr<Guid>> AddAsync(TestSchool school, Grade grade, Subject subject, DeliveryMode mode, int hours,
        Guid? shift = null, Guid? into = null, string? note = null) =>
        _host.SendAsync(new AddStudyPlanItemCommand(grade.Id.Value, subject.Id.Value, mode, hours, shift, into, note)
        {
            StudyPlanId = school.Plan.Id.Value
        });

    #region Consulta

    [Fact]
    public async Task GetStudyPlan_ShouldReturnTheMatrixOfAreasSubjectsAndGrades_WithTotals()
    {
        var school = await SeedAsync();

        var plan = await GetPlanAsync(school.Plan.Id.Value);

        plan.Name.Should().Be("Plan de prueba");
        plan.Year.Should().Be(2026);
        plan.CampusName.Should().Be("Sede Principal");
        plan.Status.Should().Be(StudyPlanStatus.Draft);
        plan.Periods.Select(p => p.Name).Should().Equal("Periodo 1", "Periodo 2");
        plan.Shifts.Select(s => s.Name).Should().Equal("Mañana", "Tarde");

        plan.Grades.Select(g => g.ShortName).Should().Equal("6º", "7º");
        plan.Grades.Should().OnlyContain(g => g.WeeklyTotal == TestSchool.BaseWeeklyHours && g.CounterShiftTotal == 0);
        plan.Grades.Should().OnlyContain(g => g.PeriodTotals.Count == 2 && g.PeriodTotals.All(p => p.WeeklyTotal == TestSchool.BaseWeeklyHours));

        plan.Areas.Select(a => a.Name).Should().Equal(
            "Matemáticas", "Humanidades", "Ciencias Naturales", "Tecnología e Informática", "Educación Física", "Ciencias Sociales");
        plan.Areas[1].Subjects.Select(s => s.Name).Should().Equal("Lengua castellana", "Inglés");

        // Las asignaturas activas sin IH también salen, para poder llenarlas
        plan.Areas.Single(a => a.Name == "Ciencias Sociales").Subjects.Single().Items.Should().BeEmpty();
        plan.Areas.Single(a => a.Name == "Ciencias Naturales").Subjects.Single(s => s.Name == "Proyecto de investigación").Items.Should().BeEmpty();

        var english = ItemOf(plan, school.English, school.Seventh);
        english.WeeklyHours.Should().Be(4);
        english.DeliveryMode.Should().Be(DeliveryMode.Regular);
        english.MaxHoursPerDay.Should().Be(2);
        english.MaxConsecutiveHours.Should().Be(2);
        english.PeriodHours.Should().BeEmpty();

        plan.Areas.SelectMany(a => a.Subjects).Sum(s => s.Items.Count).Should().Be(12);
    }

    [Fact]
    public async Task GetStudyPlan_ShouldHideInactiveSubjectsWithoutHours_ButKeepInactiveOnesThatAreInThePlan()
    {
        var school = await SeedAsync(s =>
        {
            s.Ethics.SetSubjectActive(s.Citizenship.Id, false);
            s.Technology.SetSubjectActive(s.Informatics.Id, false);
        });

        var plan = await GetPlanAsync(school.Plan.Id.Value);

        plan.Areas.Should().NotContain(a => a.Name == "Ciencias Sociales", "su única asignatura está inactiva y no tiene IH");
        var informatics = plan.Areas.Single(a => a.Name == "Tecnología e Informática").Subjects.Single();
        informatics.IsActive.Should().BeFalse();
        informatics.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStudyPlan_ThatDoesNotExist_ShouldReturnNotFound()
    {
        await SeedAsync();

        var result = await _host.SendAsync(new GetStudyPlanQuery(Guid.NewGuid()));

        result.FirstError.Should().Be(StudyPlanErrors.NotFound);
    }

    [Fact]
    public async Task GetStudyPlans_ShouldFilterByYearAndCampus()
    {
        var school = await SeedAsync();
        var nextYear = await SeedNextYearAsync();
        await _host.SendAsync(new CreateStudyPlanCommand(nextYear.Id.Value, school.Campus.Id.Value));
        await _host.SendAsync(new CreateStudyPlanCommand(school.Year.Id.Value, school.OtherCampus.Id.Value));

        var all = await _host.SendAsync(new GetStudyPlansQuery());
        var campus2026 = await _host.SendAsync(new GetStudyPlansQuery(school.Year.Id.Value, school.Campus.Id.Value));
        var other = await _host.SendAsync(new GetStudyPlansQuery(CampusId: school.OtherCampus.Id.Value));

        all.Should().HaveCount(3);
        all[0].Year.Should().Be(2027, "el más reciente primero");
        campus2026.Should().ContainSingle().Which.Should().BeEquivalentTo(new StudyPlanSummaryDto(
            school.Plan.Id.Value, school.Year.Id.Value, 2026, school.Campus.Id.Value, "Sede Principal",
            "Plan de prueba", StudyPlanStatus.Draft, null, 12));
        other.Should().ContainSingle().Which.SubjectCount.Should().Be(0);
    }

    #endregion

    #region Crear y copiar

    [Fact]
    public async Task CreateStudyPlan_Empty_ShouldUseADefaultNameAndStartAsADraftWithoutSubjects()
    {
        var school = await SeedAsync();
        var nextYear = await SeedNextYearAsync();

        var result = await _host.SendAsync(new CreateStudyPlanCommand(nextYear.Id.Value, school.Campus.Id.Value));

        result.IsError.Should().BeFalse();
        var plan = await GetPlanAsync(result.Value);
        plan.Name.Should().Be("Plan de estudios 2027 - Sede Principal");
        plan.Status.Should().Be(StudyPlanStatus.Draft);
        plan.Areas.SelectMany(a => a.Subjects).Should().OnlyContain(s => s.Items.Count == 0);
        plan.Grades.Should().OnlyContain(g => g.WeeklyTotal == 0);
    }

    [Fact]
    public async Task CreateStudyPlan_WhenTheCampusAlreadyHasAPlanThatYear_ShouldReturnAlreadyExists()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new CreateStudyPlanCommand(school.Year.Id.Value, school.Campus.Id.Value, "Otro"));

        result.FirstError.Should().Be(StudyPlanErrors.AlreadyExists);
        (await _host.SendAsync(new GetStudyPlansQuery())).Should().ContainSingle();
    }

    [Fact]
    public async Task CreateStudyPlan_WithUnknownReferences_ShouldReturnTheMatchingNotFound()
    {
        var school = await SeedAsync();
        var nextYear = await SeedNextYearAsync();

        var noYear = await _host.SendAsync(new CreateStudyPlanCommand(Guid.NewGuid(), school.Campus.Id.Value));
        var noCampus = await _host.SendAsync(new CreateStudyPlanCommand(school.Year.Id.Value, Guid.NewGuid()));
        var noSource = await _host.SendAsync(new CreateStudyPlanCommand(nextYear.Id.Value, school.Campus.Id.Value, CopyFromStudyPlanId: Guid.NewGuid()));

        noYear.FirstError.Should().Be(AcademicYearErrors.NotFound);
        noCampus.FirstError.Should().Be(CampusErrors.NotFound);
        noSource.FirstError.Should().Be(StudyPlanErrors.NotFound);
        (await _host.SendAsync(new GetStudyPlansQuery())).Should().ContainSingle();
    }

    [Fact]
    public async Task CreateStudyPlan_CopyingToTheNextYear_ShouldCopyEverySubjectWithItsRules_ButNotThePeriodAdjustments()
    {
        var school = await SeedAsync(s =>
        {
            var english = s.Item(s.Sixth, s.English);
            s.Plan.SetItemPeriodHours(english.Id, s.Period2.Id, 3).IsError.Should().BeFalse();
            s.Plan.SetItemDistribution(s.Item(s.Sixth, s.Informatics).Id, 1, 1, SpaceType.ComputerLab).IsError.Should().BeFalse();
            s.Plan.AddItem(s.Sixth.Id, s.Citizenship.Id, DeliveryMode.Transversal, 0, null, s.Spanish.Id, "Se integra en Lengua").IsError.Should().BeFalse();
            s.Plan.AddItem(s.Seventh.Id, s.Research.Id, DeliveryMode.CounterShift, 2, s.Afternoon.Id).IsError.Should().BeFalse();
            s.Plan.Update("Plan de prueba", "Notas al pie").IsError.Should().BeFalse();
            s.Plan.Approve(DateTimeOffset.UtcNow).IsError.Should().BeFalse();
        });
        var nextYear = await SeedNextYearAsync();

        var result = await _host.SendAsync(new CreateStudyPlanCommand(nextYear.Id.Value, school.Campus.Id.Value, "Plan 2027", school.Plan.Id.Value));

        result.IsError.Should().BeFalse();
        var source = await GetPlanAsync(school.Plan.Id.Value);
        var copy = await GetPlanAsync(result.Value);

        copy.Name.Should().Be("Plan 2027");
        copy.Notes.Should().Be("Notas al pie");
        copy.Status.Should().Be(StudyPlanStatus.Draft, "la copia se revisa antes de aprobarla");

        var sourceItems = source.Areas.SelectMany(a => a.Subjects).SelectMany(s => s.Items).ToList();
        var copyItems = copy.Areas.SelectMany(a => a.Subjects).SelectMany(s => s.Items).ToList();
        copyItems.Should().HaveCount(14);
        copyItems.Select(i => i.Id).Should().NotIntersectWith(sourceItems.Select(i => i.Id));
        copyItems.Should().BeEquivalentTo(sourceItems, o => o.Excluding(i => i.Id).Excluding(i => i.PeriodHours));
        copyItems.Should().OnlyContain(i => i.PeriodHours.Count == 0, "los periodos del año nuevo son otros");

        var informatics = ItemOf(copy, school.Informatics, school.Sixth);
        informatics.RequiredSpaceType.Should().Be(SpaceType.ComputerLab);
        var citizenship = ItemOf(copy, school.Citizenship, school.Sixth);
        citizenship.IntegratedIntoSubjectId.Should().Be(school.Spanish.Id.Value);
        citizenship.Note.Should().Be("Se integra en Lengua");
        ItemOf(copy, school.Research, school.Seventh).TargetShiftId.Should().Be(school.Afternoon.Id.Value);

        GradeOf(copy, school.Seventh).CounterShiftTotal.Should().Be(2);
        copy.Grades.Should().OnlyContain(g => g.WeeklyTotal == TestSchool.BaseWeeklyHours);

        // El plan de origen no cambia
        source.Status.Should().Be(StudyPlanStatus.Approved);
        ItemOf(source, school.English, school.Sixth).PeriodHours.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateStudyPlan_CopyingToAnotherCampus_ShouldLinkCounterShiftSubjectsToTheShiftWithTheSameName()
    {
        var school = await SeedAsync(s =>
            s.Plan.AddItem(s.Sixth.Id, s.Research.Id, DeliveryMode.CounterShift, 2, s.Morning.Id).IsError.Should().BeFalse());

        var result = await _host.SendAsync(new CreateStudyPlanCommand(school.Year.Id.Value, school.OtherCampus.Id.Value,
            CopyFromStudyPlanId: school.Plan.Id.Value));

        result.IsError.Should().BeFalse();
        var copy = await GetPlanAsync(result.Value);
        copy.Name.Should().Be("Plan de estudios 2026 - Sede Norte");
        ItemOf(copy, school.Research, school.Sixth).TargetShiftId.Should().Be(school.OtherMorning.Id.Value);
        copy.Areas.SelectMany(a => a.Subjects).Sum(s => s.Items.Count).Should().Be(13);
    }

    [Fact]
    public async Task CreateStudyPlan_CopyingToACampusWithoutTheCounterShift_ShouldFailAndSaveNothing()
    {
        var school = await SeedAsync(s =>
            s.Plan.AddItem(s.Sixth.Id, s.Research.Id, DeliveryMode.CounterShift, 2, s.Afternoon.Id).IsError.Should().BeFalse());

        var result = await _host.SendAsync(new CreateStudyPlanCommand(school.Year.Id.Value, school.OtherCampus.Id.Value,
            CopyFromStudyPlanId: school.Plan.Id.Value));

        result.FirstError.Should().Be(StudyPlanErrors.ShiftNotInCampus);
        (await _host.SendAsync(new GetStudyPlansQuery())).Should().ContainSingle();
    }

    [Fact]
    public async Task CreateStudyPlan_WithInvalidData_ShouldReturnValidationErrors()
    {
        await SeedAsync();

        var result = await _host.SendAsync(new CreateStudyPlanCommand(Guid.Empty, Guid.Empty, new string('x', StudyPlan.NameMaxLength + 1), Guid.Empty));

        result.Errors.Should().HaveCount(4).And.OnlyContain(e => e.Type == ErrorType.Validation);
    }

    [Fact]
    public async Task UpdateStudyPlan_ShouldChangeNameAndNotes()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new UpdateStudyPlanCommand("Plan 2026 ajustado", "  Nota  ") { StudyPlanId = school.Plan.Id.Value });

        result.IsError.Should().BeFalse();
        var plan = await GetPlanAsync(school.Plan.Id.Value);
        plan.Name.Should().Be("Plan 2026 ajustado");
        plan.Notes.Should().Be("Nota");
    }

    #endregion

    #region Asignaturas del plan

    [Fact]
    public async Task AddItem_Regular_ShouldAddTheSubjectToTheGradeAndUpdateOnlyThatGradesTotal()
    {
        var school = await SeedAsync();

        var result = await AddAsync(school, school.Sixth, school.Research, DeliveryMode.Regular, 2, note: "Semillero");

        result.IsError.Should().BeFalse();
        var plan = await GetPlanAsync(school.Plan.Id.Value);
        var item = ItemOf(plan, school.Research, school.Sixth);
        item.Id.Should().Be(result.Value);
        item.WeeklyHours.Should().Be(2);
        item.Note.Should().Be("Semillero");
        item.MaxHoursPerDay.Should().BeNull();
        GradeOf(plan, school.Sixth).WeeklyTotal.Should().Be(TestSchool.BaseWeeklyHours + 2);
        GradeOf(plan, school.Sixth).PeriodTotals.Should().OnlyContain(p => p.WeeklyTotal == TestSchool.BaseWeeklyHours + 2);
        GradeOf(plan, school.Seventh).WeeklyTotal.Should().Be(TestSchool.BaseWeeklyHours);
    }

    [Fact]
    public async Task AddItem_CounterShift_ShouldCountInTheCounterShiftTotalNotInTheRegularOne()
    {
        var school = await SeedAsync();

        var result = await AddAsync(school, school.Seventh, school.Research, DeliveryMode.CounterShift, 3, school.Afternoon.Id.Value);

        result.IsError.Should().BeFalse();
        var plan = await GetPlanAsync(school.Plan.Id.Value);
        ItemOf(plan, school.Research, school.Seventh).TargetShiftId.Should().Be(school.Afternoon.Id.Value);
        GradeOf(plan, school.Seventh).WeeklyTotal.Should().Be(TestSchool.BaseWeeklyHours);
        GradeOf(plan, school.Seventh).CounterShiftTotal.Should().Be(3);
    }

    [Fact]
    public async Task AddItem_Transversal_ShouldHaveNoHoursAndKeepTheSubjectItIsIntegratedInto()
    {
        var school = await SeedAsync();

        var result = await AddAsync(school, school.Sixth, school.Citizenship, DeliveryMode.Transversal, 3, into: school.Spanish.Id.Value);

        result.IsError.Should().BeFalse();
        var plan = await GetPlanAsync(school.Plan.Id.Value);
        var item = ItemOf(plan, school.Citizenship, school.Sixth);
        item.WeeklyHours.Should().Be(0, "una transversal no ocupa franjas");
        item.IntegratedIntoSubjectId.Should().Be(school.Spanish.Id.Value);
        GradeOf(plan, school.Sixth).WeeklyTotal.Should().Be(TestSchool.BaseWeeklyHours);
    }

    [Fact]
    public async Task AddItem_ThatBreaksARule_ShouldReturnTheMatchingErrorAndChangeNothing()
    {
        var school = await SeedAsync(s => s.Science.SetSubjectActive(s.Research.Id, false));
        var unknown = Guid.NewGuid();

        var duplicate = await AddAsync(school, school.Sixth, school.English, DeliveryMode.Regular, 2);
        var inactive = await AddAsync(school, school.Sixth, school.Research, DeliveryMode.Regular, 2);
        var noShift = await AddAsync(school, school.Sixth, school.Citizenship, DeliveryMode.CounterShift, 2);
        var otherCampusShift = await AddAsync(school, school.Sixth, school.Citizenship, DeliveryMode.CounterShift, 2, school.OtherMorning.Id.Value);
        var intoItself = await AddAsync(school, school.Sixth, school.Citizenship, DeliveryMode.Transversal, 0, into: school.Citizenship.Id.Value);
        var intoUnknown = await AddAsync(school, school.Sixth, school.Citizenship, DeliveryMode.Transversal, 0, into: unknown);
        var unknownGrade = await _host.SendAsync(new AddStudyPlanItemCommand(unknown, school.Citizenship.Id.Value, DeliveryMode.Regular, 1)
        {
            StudyPlanId = school.Plan.Id.Value
        });
        var unknownSubject = await _host.SendAsync(new AddStudyPlanItemCommand(school.Sixth.Id.Value, unknown, DeliveryMode.Regular, 1)
        {
            StudyPlanId = school.Plan.Id.Value
        });
        var unknownPlan = await _host.SendAsync(new AddStudyPlanItemCommand(school.Sixth.Id.Value, school.Citizenship.Id.Value, DeliveryMode.Regular, 1)
        {
            StudyPlanId = unknown
        });

        duplicate.FirstError.Should().Be(StudyPlanErrors.DuplicateItem);
        inactive.FirstError.Should().Be(StudyPlanErrors.SubjectInactive);
        noShift.FirstError.Should().Be(StudyPlanErrors.CounterShiftRequiresShift);
        otherCampusShift.FirstError.Should().Be(StudyPlanErrors.ShiftNotInCampus);
        intoItself.FirstError.Should().Be(StudyPlanErrors.IntegratedIntoItself);
        intoUnknown.FirstError.Should().Be(AreaErrors.SubjectNotFound);
        unknownGrade.FirstError.Should().Be(GradeErrors.NotFound);
        unknownSubject.FirstError.Should().Be(AreaErrors.SubjectNotFound);
        unknownPlan.FirstError.Should().Be(StudyPlanErrors.NotFound);

        var plan = await GetPlanAsync(school.Plan.Id.Value);
        plan.Areas.SelectMany(a => a.Subjects).Sum(s => s.Items.Count).Should().Be(12);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(StudyPlanItem.MaxWeeklyHours + 1)]
    public async Task AddItem_WithHoursOutOfRange_ShouldReturnAValidationError(int hours)
    {
        var school = await SeedAsync();

        var result = await AddAsync(school, school.Sixth, school.Research, DeliveryMode.Regular, hours);

        result.Errors.Should().ContainSingle().Which.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task UpdateItem_ShouldChangeHoursAndDeliveryMode()
    {
        var school = await SeedAsync();
        var itemId = school.Item(school.Sixth, school.English).Id.Value;

        var toCounterShift = await _host.SendAsync(new UpdateStudyPlanItemCommand(DeliveryMode.CounterShift, 3, school.Afternoon.Id.Value, Note: "En la tarde")
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = itemId
        });

        toCounterShift.IsError.Should().BeFalse();
        var plan = await GetPlanAsync(school.Plan.Id.Value);
        var item = ItemOf(plan, school.English, school.Sixth);
        item.DeliveryMode.Should().Be(DeliveryMode.CounterShift);
        item.WeeklyHours.Should().Be(3);
        item.TargetShiftId.Should().Be(school.Afternoon.Id.Value);
        item.Note.Should().Be("En la tarde");
        GradeOf(plan, school.Sixth).WeeklyTotal.Should().Be(TestSchool.BaseWeeklyHours - 4);
        GradeOf(plan, school.Sixth).CounterShiftTotal.Should().Be(3);

        var backToRegular = await _host.SendAsync(new UpdateStudyPlanItemCommand(DeliveryMode.Regular, 5, school.Afternoon.Id.Value)
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = itemId
        });

        backToRegular.IsError.Should().BeFalse();
        plan = await GetPlanAsync(school.Plan.Id.Value);
        item = ItemOf(plan, school.English, school.Sixth);
        item.TargetShiftId.Should().BeNull("una asignatura regular se dicta en la jornada del curso");
        item.Note.Should().BeNull();
        GradeOf(plan, school.Sixth).WeeklyTotal.Should().Be(TestSchool.BaseWeeklyHours + 1);
        GradeOf(plan, school.Sixth).CounterShiftTotal.Should().Be(0);
    }

    [Fact]
    public async Task UpdateItem_ThatDoesNotExistOrWithAShiftOfAnotherCampus_ShouldFail()
    {
        var school = await SeedAsync();

        var missing = await _host.SendAsync(new UpdateStudyPlanItemCommand(DeliveryMode.Regular, 3)
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = Guid.NewGuid()
        });
        var otherCampus = await _host.SendAsync(new UpdateStudyPlanItemCommand(DeliveryMode.CounterShift, 3, school.OtherMorning.Id.Value)
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = school.Item(school.Sixth, school.English).Id.Value
        });

        missing.FirstError.Should().Be(StudyPlanErrors.ItemNotFound);
        otherCampus.FirstError.Should().Be(StudyPlanErrors.ShiftNotInCampus);
        ItemOf(await GetPlanAsync(school.Plan.Id.Value), school.English, school.Sixth).DeliveryMode.Should().Be(DeliveryMode.Regular);
    }

    [Fact]
    public async Task SetDistribution_ShouldSaveTheRules_AndRejectMoreConsecutiveHoursThanPerDay()
    {
        var school = await SeedAsync();
        var itemId = school.Item(school.Seventh, school.Biology).Id.Value;

        var ok = await _host.SendAsync(new SetStudyPlanItemDistributionCommand(3, 2, SpaceType.Laboratory)
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = itemId
        });
        var invalid = await _host.SendAsync(new SetStudyPlanItemDistributionCommand(1, 2, null)
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = itemId
        });

        ok.IsError.Should().BeFalse();
        invalid.FirstError.Should().Be(StudyPlanErrors.InvalidDistribution);
        var item = ItemOf(await GetPlanAsync(school.Plan.Id.Value), school.Biology, school.Seventh);
        item.MaxHoursPerDay.Should().Be(3);
        item.MaxConsecutiveHours.Should().Be(2);
        item.RequiredSpaceType.Should().Be(SpaceType.Laboratory);

        var cleared = await _host.SendAsync(new SetStudyPlanItemDistributionCommand(null, null, null)
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = itemId
        });
        cleared.IsError.Should().BeFalse();
        item = ItemOf(await GetPlanAsync(school.Plan.Id.Value), school.Biology, school.Seventh);
        item.MaxHoursPerDay.Should().BeNull();
        item.MaxConsecutiveHours.Should().BeNull();
        item.RequiredSpaceType.Should().BeNull();
    }

    [Fact]
    public async Task SetPeriodHours_ShouldChangeOnlyThatPeriodsTotal_AndNullShouldGoBackToTheGeneralHours()
    {
        var school = await SeedAsync();
        var itemId = school.Item(school.Sixth, school.Arithmetic).Id.Value;

        var set = await _host.SendAsync(new SetStudyPlanItemPeriodHoursCommand(3)
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = itemId,
            AcademicPeriodId = school.Period2.Id.Value
        });

        set.IsError.Should().BeFalse();
        var plan = await GetPlanAsync(school.Plan.Id.Value);
        ItemOf(plan, school.Arithmetic, school.Sixth).PeriodHours.Should().Equal(new StudyPlanPeriodHoursDto(school.Period2.Id.Value, 3));
        var sixth = GradeOf(plan, school.Sixth);
        sixth.WeeklyTotal.Should().Be(TestSchool.BaseWeeklyHours, "el total general usa la IH general");
        sixth.PeriodTotals.Single(p => p.AcademicPeriodId == school.Period1.Id.Value).WeeklyTotal.Should().Be(TestSchool.BaseWeeklyHours);
        sixth.PeriodTotals.Single(p => p.AcademicPeriodId == school.Period2.Id.Value).WeeklyTotal.Should().Be(TestSchool.BaseWeeklyHours - 2);

        var changed = await _host.SendAsync(new SetStudyPlanItemPeriodHoursCommand(6)
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = itemId,
            AcademicPeriodId = school.Period2.Id.Value
        });
        changed.IsError.Should().BeFalse();
        ItemOf(await GetPlanAsync(school.Plan.Id.Value), school.Arithmetic, school.Sixth).PeriodHours
            .Should().Equal(new StudyPlanPeriodHoursDto(school.Period2.Id.Value, 6));

        var cleared = await _host.SendAsync(new SetStudyPlanItemPeriodHoursCommand(null)
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = itemId,
            AcademicPeriodId = school.Period2.Id.Value
        });

        cleared.IsError.Should().BeFalse();
        plan = await GetPlanAsync(school.Plan.Id.Value);
        ItemOf(plan, school.Arithmetic, school.Sixth).PeriodHours.Should().BeEmpty();
        GradeOf(plan, school.Sixth).PeriodTotals.Should().OnlyContain(p => p.WeeklyTotal == TestSchool.BaseWeeklyHours);
    }

    [Fact]
    public async Task SetPeriodHours_ForAPeriodOfAnotherYearOrATransversalSubject_ShouldFail()
    {
        var school = await SeedAsync(s =>
            s.Plan.AddItem(s.Sixth.Id, s.Citizenship.Id, DeliveryMode.Transversal, 0, null, s.Spanish.Id).IsError.Should().BeFalse());
        var nextYear = await SeedNextYearAsync();

        var otherYear = await _host.SendAsync(new SetStudyPlanItemPeriodHoursCommand(3)
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = school.Item(school.Sixth, school.Arithmetic).Id.Value,
            AcademicPeriodId = nextYear.Periods[0].Id.Value
        });
        var transversal = await _host.SendAsync(new SetStudyPlanItemPeriodHoursCommand(3)
        {
            StudyPlanId = school.Plan.Id.Value,
            ItemId = school.Item(school.Sixth, school.Citizenship).Id.Value,
            AcademicPeriodId = school.Period1.Id.Value
        });

        otherYear.FirstError.Should().Be(StudyPlanErrors.PeriodNotInYear);
        transversal.FirstError.Should().Be(StudyPlanErrors.TransversalHasNoHours);
    }

    [Fact]
    public async Task RemoveItem_ShouldTakeTheSubjectOutOfTheGradeOnly()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new RemoveStudyPlanItemCommand(school.Plan.Id.Value, school.Item(school.Sixth, school.Informatics).Id.Value));
        var again = await _host.SendAsync(new RemoveStudyPlanItemCommand(school.Plan.Id.Value, school.Item(school.Sixth, school.Informatics).Id.Value));

        result.IsError.Should().BeFalse();
        again.FirstError.Should().Be(StudyPlanErrors.ItemNotFound);
        var plan = await GetPlanAsync(school.Plan.Id.Value);
        plan.Areas.SelectMany(a => a.Subjects).Single(s => s.Id == school.Informatics.Id.Value)
            .Items.Should().ContainSingle().Which.GradeId.Should().Be(school.Seventh.Id.Value);
        GradeOf(plan, school.Sixth).WeeklyTotal.Should().Be(TestSchool.BaseWeeklyHours - 2);
        GradeOf(plan, school.Seventh).WeeklyTotal.Should().Be(TestSchool.BaseWeeklyHours);
    }

    #endregion

    #region Aprobación

    [Fact]
    public async Task ApprovedPlan_ShouldRejectEveryChange_UntilItIsReopened()
    {
        var school = await SeedAsync();
        var planId = school.Plan.Id.Value;
        var itemId = school.Item(school.Sixth, school.English).Id.Value;

        var approve = await _host.SendAsync(new ApproveStudyPlanCommand(planId));
        var approveAgain = await _host.SendAsync(new ApproveStudyPlanCommand(planId));

        approve.IsError.Should().BeFalse();
        approveAgain.FirstError.Should().Be(StudyPlanErrors.AlreadyApproved);
        var approved = await GetPlanAsync(planId);
        approved.Status.Should().Be(StudyPlanStatus.Approved);
        approved.ApprovedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        var errors = new List<Error>
        {
            (await _host.SendAsync(new UpdateStudyPlanCommand("Otro", null) { StudyPlanId = planId })).FirstError,
            (await AddAsync(school, school.Sixth, school.Research, DeliveryMode.Regular, 1)).FirstError,
            (await _host.SendAsync(new UpdateStudyPlanItemCommand(DeliveryMode.Regular, 1) { StudyPlanId = planId, ItemId = itemId })).FirstError,
            (await _host.SendAsync(new SetStudyPlanItemDistributionCommand(1, 1, null) { StudyPlanId = planId, ItemId = itemId })).FirstError,
            (await _host.SendAsync(new SetStudyPlanItemPeriodHoursCommand(1)
            {
                StudyPlanId = planId,
                ItemId = itemId,
                AcademicPeriodId = school.Period1.Id.Value
            })).FirstError,
            (await _host.SendAsync(new RemoveStudyPlanItemCommand(planId, itemId))).FirstError,
        };

        errors.Should().HaveCount(6).And.AllBeEquivalentTo(StudyPlanErrors.NotEditable);
        (await GetPlanAsync(planId)).Should().BeEquivalentTo(approved, "nada cambió");

        var reopen = await _host.SendAsync(new ReopenStudyPlanCommand(planId));
        var edit = await _host.SendAsync(new UpdateStudyPlanItemCommand(DeliveryMode.Regular, 1) { StudyPlanId = planId, ItemId = itemId });

        reopen.IsError.Should().BeFalse();
        edit.IsError.Should().BeFalse();
        var reopened = await GetPlanAsync(planId);
        reopened.Status.Should().Be(StudyPlanStatus.Draft);
        reopened.ApprovedAt.Should().BeNull();
        ItemOf(reopened, school.English, school.Sixth).WeeklyHours.Should().Be(1);
    }

    [Fact]
    public async Task Approve_AnEmptyPlan_ShouldFail()
    {
        var school = await SeedAsync();
        var nextYear = await SeedNextYearAsync();
        var empty = await _host.SendAsync(new CreateStudyPlanCommand(nextYear.Id.Value, school.Campus.Id.Value));

        var result = await _host.SendAsync(new ApproveStudyPlanCommand(empty.Value));
        var missing = await _host.SendAsync(new ApproveStudyPlanCommand(Guid.NewGuid()));

        result.FirstError.Should().Be(StudyPlanErrors.Empty);
        missing.FirstError.Should().Be(StudyPlanErrors.NotFound);
    }

    #endregion

    #region Del plan al horario

    [Fact]
    public async Task ChangesMadeThroughTheApi_ShouldBeWhatTheGeneratorSchedules()
    {
        var school = await SeedAsync();
        var planId = school.Plan.Id.Value;

        // 6º deja Informática (−2) y sube Inglés a 5 h (+1); 7º recibe Proyecto de investigación (2 h)
        (await _host.SendAsync(new RemoveStudyPlanItemCommand(planId, school.Item(school.Sixth, school.Informatics).Id.Value))).IsError.Should().BeFalse();
        (await _host.SendAsync(new UpdateStudyPlanItemCommand(DeliveryMode.Regular, 5)
        {
            StudyPlanId = planId,
            ItemId = school.Item(school.Sixth, school.English).Id.Value
        })).IsError.Should().BeFalse();
        (await AddAsync(school, school.Seventh, school.Research, DeliveryMode.Regular, 2)).IsError.Should().BeFalse();
        (await _host.SendAsync(new ApproveStudyPlanCommand(planId))).IsError.Should().BeFalse();

        var job = GenerationJob.Create(school.Timetable.Id, DateTimeOffset.UtcNow, 30);
        await _host.SeedAsync(db => db.GenerationJobs.Add(job));
        await _host.RunJobAsync(job.Id);

        var (finished, lessons) = await _host.WithDbAsync(async db => (
            await db.GenerationJobs.FirstAsync(j => j.Id == job.Id),
            (await db.Timetables.Include(t => t.Lessons).FirstAsync(t => t.Id == school.Timetable.Id)).Lessons.ToList()));

        finished.Status.Should().Be(GenerationJobStatus.Succeeded, finished.Message);
        foreach (var course in school.CoursesOf(school.Sixth))
        {
            var own = lessons.Where(l => l.CourseId == course.Id).ToList();
            own.Should().HaveCount(TestSchool.BaseWeeklyHours - 1);
            own.Should().NotContain(l => l.SubjectId == school.Informatics.Id);
            own.Count(l => l.SubjectId == school.English.Id).Should().Be(5);
        }

        foreach (var course in school.CoursesOf(school.Seventh))
        {
            var own = lessons.Where(l => l.CourseId == course.Id).ToList();
            own.Should().HaveCount(TestSchool.BaseWeeklyHours + 2);
            own.Count(l => l.SubjectId == school.Research.Id).Should().Be(2);
        }
    }

    #endregion
}
