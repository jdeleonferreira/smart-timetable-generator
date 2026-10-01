# SmartTimetableGenerator - Testing Patterns

This file covers all testing patterns across integration, unit, and architecture test projects.

## Shared Conventions

- **Awesome Assertions** - Use `Should()` syntax for all assertions

## Integration Tests

Location: `tests/WebApi.IntegrationTests/Endpoints/{Feature}/`

Uses TestContainers (real database) + Respawn (database reset between tests).

```csharp
public class CreateTimetableCommandTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Command_ShouldCreateTimetable()
    {
        // Arrange: seed year, campus, study plan with AddAsync(...)
        var cmd = new CreateTimetableCommand(yearId, campusId, periodId, null);
        var client = GetAnonymousClient();

        var result = await client.PostAsJsonAsync("/api/timetables", cmd, CancellationToken);

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var timetable = await GetQueryable<Timetable>().FirstAsync(CancellationToken);
        timetable.Status.Should().Be(TimetableStatus.Draft);
    }
}
```

Key helpers available from `IntegrationTestBase`:
- `GetAnonymousClient()` - unauthenticated `HttpClient`
- `GetQueryable<T>()` - direct EF Core access to verify database state

## Unit Tests

Location: `tests/Domain.UnitTests/`

Test domain logic directly — no EF Core, no mocking of infrastructure. Focus on:
- Aggregate behaviour and invariants
- Specifications (e.g., `TimetableSpec.ById`)
- Domain event raising

## Architecture Tests

Location: `tests/Architecture.Tests/`

Uses NetArchTest to enforce layer dependency rules. Ensures no illegal references between layers (e.g., Domain must not reference Application or Infrastructure).

## Scheduling Tests

Location: `tests/Scheduling.Tests/`

- `Solver/` - CP-SAT engine scenarios and seeded random problems, every solution checked by `SolutionValidator`
- `Generation/` - real application (MediatR, validators, EF Core on in-memory SQLite, CP-SAT) over `TestSchool`,
  every timetable checked by `TimetableValidator`
