# SmartTimetableGenerator - Implementation Patterns

This file covers conventions, migrations, and all coding patterns for the Domain, Application, Infrastructure, and WebApi layers.

## Conventions

- **No AutoMapper** - Use manual mapping with `Select()` projections
- **Strongly-typed IDs** - All entities use Vogen `[ValueObject<Guid>]`
- **Factory methods** - Create aggregates via static `Create()` methods, not constructors
- **Specifications** - One class per aggregate in Domain layer (`{Aggregate}Spec`), with static factory methods per query
- **FluentValidation** - Validators in same folder as Command/Query
- **Awesome Assertions** - Use `Should()` syntax in tests
- **Domain model** - See `docs/domain.md` (glossary Spanish ↔ code, rules)

## EF Migrations

```bash
# Add migration
dotnet ef migrations add YourMigration --project ./src/Infrastructure --startup-project ./src/WebApi --output-dir ./Persistence/Migrations

# Migrations apply automatically via Aspire MigrationService
```

## Commands (Create/Update/Delete)

Location: `src/Application/UseCases/{Feature}/Commands/{CommandName}/`

```csharp
public sealed record CreateSpaceCommand(Guid CampusId, string Name, SpaceType Type) : IRequest<ErrorOr<Guid>>;

internal sealed class CreateSpaceCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateSpaceCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        var space = Space.Create(CampusId.From(request.CampusId), request.Name, request.Type);
        dbContext.Spaces.Add(space);
        await dbContext.SaveChangesAsync(ct);
        return space.Id.Value;
    }
}

internal sealed class CreateSpaceCommandValidator : AbstractValidator<CreateSpaceCommand>
{
    public CreateSpaceCommandValidator() => RuleFor(v => v.Name).NotEmpty();
}
```

## Authorization

- Every command (except `LoginCommand`) must have `[Authorize]` (`Application/Common/Security`); an architecture test enforces it.
  Use `Roles = Roles.Admin` for institutional catalogs and users, `Roles = Roles.Managers` for study plans and timetables.
- Requests with `[Authorize]` must return `ErrorOr<T>` (the `AuthorizationBehaviour` answers 401/403 with errors).
- Campus scope: after loading the resource, `if (currentUser.EnsureCanManageCampus(x.CampusId) is { } forbidden) return forbidden;`
- Teachers only see published timetables (`currentUser.CanSeeDrafts()`).
- In tests, `SchedulingTestHost.SignInAs(role, campusId, teacherId)` switches the user (default: Admin).

## Queries (Read)

Location: `src/Application/UseCases/{Feature}/Queries/{QueryName}/`

```csharp
public record GetGradesQuery : IRequest<IReadOnlyList<GradeDto>>;
public record GradeDto(Guid Id, string Name, int Order);

internal sealed class GetGradesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetGradesQuery, IReadOnlyList<GradeDto>>
{
    public async Task<IReadOnlyList<GradeDto>> Handle(GetGradesQuery request, CancellationToken ct)
        => await dbContext.Grades.OrderBy(g => g.Order).Select(g => new GradeDto(g.Id.Value, g.Name, g.Order)).ToListAsync(ct);
}
```

## Domain Entities with Strongly-Typed IDs (Vogen)

Location: `src/Domain/{Feature}/`

```csharp
[ValueObject<Guid>]
public readonly partial struct GradeId;

public class Grade : AggregateRoot<GradeId>
{
    private Grade() { }  // EF Core constructor

    public static Grade Create(string name, string shortName, EducationLevel level, int order)
    {
        var grade = new Grade { Id = GradeId.From(Guid.CreateVersion7()) };
        grade.Update(name, shortName, level, order);
        return grade;
    }
}
```

## Domain Events

Raise events from aggregates to trigger side effects. Events are dispatched after `SaveChangesAsync()`.

**Define event** in `src/Domain/{Feature}/`:
```csharp
public sealed record ProjectActivityScheduledEvent(TrainingProject Project, ProjectActivity Activity) : IDomainEvent;
```

**Raise from aggregate**:
```csharp
public ErrorOr<ProjectActivity> AddActivity(string title, DateOnly date, ...)
{
    ...
    _activities.Add(activity.Value);
    AddDomainEvent(new ProjectActivityScheduledEvent(this, activity.Value));  // Raise event
    return activity.Value;
}
```

**Handle event** in `src/Application/UseCases/{Feature}/EventHandlers/`:
```csharp
internal sealed class ProjectActivityScheduledEventHandler : INotificationHandler<ProjectActivityScheduledEvent>
{
    public Task Handle(ProjectActivityScheduledEvent notification, CancellationToken ct)
    {
        // Side effects: send emails, update read models, integrate with external systems
        return Task.CompletedTask;
    }
}
```

## Minimal API Endpoints

Location: `src/WebApi/Endpoints/`

```csharp
public static void MapSpaceEndpoints(this WebApplication app)
{
    var group = app.MapApiGroup("spaces");

    group.MapPost("/", async (ISender sender, CreateSpaceCommand command, CancellationToken ct) =>
    {
        var result = await sender.Send(command, ct);
        return result.Match(id => Results.Created($"/api/spaces/{id}", new CreatedIdDto(id)), CustomResult.Problem);
    })
    .WithName("CreateSpace")
    .ProducesPost<CreatedIdDto>();  // Typed responses: the web client (Kiota) is generated from them
}
```

## Result Pattern (ErrorOr)

Use `ErrorOr<T>` for commands, not exceptions. Handle with `.Match()` at the HTTP layer:

```csharp
result.Match(success => TypedResults.Ok(success), CustomResult.Problem);
```

## Specifications

Location: `src/Domain/{Feature}/{Aggregate}Spec.cs`

Each aggregate has a single specification class that extends `SingleResultSpecification<T>`. Static factory methods build and configure instances. This groups all queries for an aggregate in one place for discoverability.

```csharp
// src/Domain/Timetables/TimetableSpec.cs
public sealed class TimetableSpec : SingleResultSpecification<Timetable>
{
    public static TimetableSpec ById(TimetableId id)
    {
        var spec = new TimetableSpec();
        spec.Query.Where(x => x.Id == id).Include(x => x.Lessons);
        return spec;
    }

    // Add further factory methods here as new queries are needed
}
```

Usage in commands:

```csharp
await dbContext.Timetables.WithSpecification(TimetableSpec.ById(id)).FirstOrDefaultAsync(ct);
```
