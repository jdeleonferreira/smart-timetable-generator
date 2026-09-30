# Smart Timetable Generator

Generador de horarios escolares a partir del plan de estudios. Gestiona el plan de estudios por año lectivo y sede
(áreas, asignaturas, intensidad horaria por grado) y genera los horarios semanales por curso, por docente e institucionales,
además del calendario por fechas reales.

Arquitectura limpia (Clean Architecture) con patrones tácticos de DDD, CQRS con MediatR, minimal APIs, EF Core,
identificadores fuertemente tipados (Vogen), patrón resultado (ErrorOr) y .NET Aspire, sobre .NET 10.
Las decisiones de arquitectura y sus razones están en [`docs/adr`](docs/adr/README.md).

## Estructura

```
src/
  Domain/          Entidades, reglas de negocio, especificaciones
  Application/     Casos de uso (commands / queries)
  Infrastructure/  EF Core (SQL Server), servicios externos
  WebApi/          Endpoints REST
tests/             Pruebas de dominio, arquitectura e integración
tools/
  AppHost/         Orquestación con .NET Aspire (SQL Server en contenedor)
  MigrationService Aplica migraciones y carga datos de ejemplo
docs/domain.md     Modelo de dominio y glosario
```

## Requisitos

- .NET SDK 10.0.100 o superior
- Docker Desktop (Aspire levanta SQL Server en un contenedor)
- Herramienta de EF Core: `dotnet tool install --global dotnet-ef`

## Primeros pasos

```bash
# 1. Restaurar y compilar
dotnet build

# 2. Crear la migración inicial (una sola vez)
dotnet ef migrations add Initial --project ./src/Infrastructure --startup-project ./src/WebApi --output-dir ./Persistence/Migrations

# 3. Pruebas
dotnet test tests/Domain.UnitTests
dotnet test tests/Architecture.Tests
dotnet test tests/Scheduling.Tests     # generación de horarios (unos 3 minutos)

# 4. Ejecutar (desde la raíz)
dotnet run --project tools/AppHost
```

En Development, el MigrationService carga datos de ejemplo: una sede con jornada mañana, tipos de jornada A (45 min) y B (40 min),
el año lectivo 2026 con 4 periodos y festivos, el plan de estudios del documento institucional (Preescolar a 11º),
dos cursos por grado con su salón, docentes ficticios por área y los proyectos de formación.

La documentación de la API queda en `https://localhost:7255/scalar/v1`.

## Generar un horario de prueba

Con la solución corriendo (`dotnet run --project tools/AppHost`), en Scalar o con `src/WebApi/WebApi.http`:

1. `GET /api/catalog/academic-years` → copie el id del año 2026 y del **Periodo 1**.
2. `GET /api/catalog/campuses` → copie el id de la **Sede Principal**.
3. `POST /api/timetables` con `{ "academicYearId", "campusId", "academicPeriodId" }` → devuelve el id del horario.
4. `POST /api/timetables/{id}/generate` → devuelve `jobId`. La generación corre en segundo plano (hasta 180 s por defecto;
   termina antes si encuentra la solución óptima).
5. `GET /api/generation-jobs/{jobId}` → repita hasta que `isFinished` sea `true`. `message` resume el resultado.
6. `GET /api/timetables/{id}/lessons?courseId=…` (por curso), `?teacherId=…` (por docente) o `?day=Wednesday` (institucional).
7. `POST /api/timetables/{id}/publish` para publicarlo.

### Cómo genera

1. **Asignación académica**: respeta las asignaciones manuales; para el resto propone un docente del área de la asignatura,
   con carga disponible, prefiriendo el mismo docente para los cursos del mismo grado. Las propuestas se guardan.
2. **Espacios**: las asignaturas que requieren un tipo de espacio (ej.: sala de informática) reciben uno de ese tipo;
   las demás usan el salón del curso.
3. **Programación (CP-SAT)**, por jornada: intensidad horaria exacta, un curso/docente/espacio por franja, máximo de horas
   por día y seguidas, bloques dobles, carga diaria pareja del curso, máximo diario del docente, disponibilidad del docente,
   clases del docente en otras sedes, clases fijadas y asignaturas en contrajornada. Minimiza huecos en la jornada del curso;
   si algo no cabe, lo deja sin ubicar y lo informa en lugar de fallar.

## Pruebas de la generación (`tests/Scheduling.Tests`)

- **Motor** (`Solver/`): escenarios con resultado conocido (bloques dobles, máximos por día y seguidos, franjas bloqueadas
  y fijadas, carga diaria, sobrecupo con el faltante exacto, casos imposibles, sin huecos) y 24 problemas aleatorios
  reproducibles. Cada solución la revisa `SolutionValidator`, que comprueba todas las reglas sin reutilizar código del motor.
- **Servicio y API** (`Generation/`): la aplicación real (MediatR, validadores, EF Core con SQLite en memoria y CP-SAT)
  sobre un colegio de prueba (`TestSchool`). Cada horario lo revisa `TimetableValidator`: intensidad horaria exacta,
  ningún curso/docente/espacio en dos lugares a la vez (también entre jornadas y sedes), área, carga semanal y diaria del
  docente, disponibilidad, espacios, contrajornada y coherencia con la asignación académica guardada.

## Licencias de terceros

Parte del código de infraestructura proviene de una plantilla publicada con licencia MIT; su aviso de copyright se
conserva en [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md), como exige esa licencia.
