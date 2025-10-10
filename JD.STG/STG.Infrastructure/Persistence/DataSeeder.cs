using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(StgDbContext db, CancellationToken ct = default)
    {
        // Evita reseed si ya existe data
        if (db.Subjects.Any()) return;

        // =========================================================
        // 1) SUBJECTS (marcando necesidades especiales)
        // =========================================================
        var subjects = new List<Subject>
        {
            // Lenguas
            new("Lengua castellana"),
            new("Inglés"),
            new("Pre-escritura"),
            new("Plan Lector"),

            // Matemáticas (MVP: consolidamos en "Matemáticas" para lower grades,
            // y añadimos ramas para upper grades)
            new("Matemáticas"),
            new("Aritmética"),
            new("Geometría"),
            new("Álgebra"),
            new("Trigonometría"),
            new("Cálculo"),
            new("Pre-matemáticas"),

            // Ciencias
            new("Ciencias naturales"),
            new("Biología", needsLab: true),
            new("Química", needsLab: true),
            new("Física", needsLab: true),

            // Sociales y humanidades
            new("Sociales"),
            new("Historia"),
            new("Geografía"),
            new("Competencia ciudadana"),
            new("Filosofía"),
            new("Ciencias económicas y políticas"),

            // Ética / Religión / Convivencia
            new("Ética y valores"),
            new("Ética empresarial"),
            new("Religión"),
            new("Cívica y Urbanidad (acompañamiento)"),

            // Artes / Música / Educación Física
            new("Artística"),
            new("Música"),
            new("Educación física y deportes"),

            // Tecnología / Informática / Proyecto / Orientación
            // Si tu Subject soporta NeedsComputerRoom, cámbialo por esa prop.:
            // new("Informática", needsComputerRoom: true)
            new("Informática", needsLab: true),
            new("Emprendimiento"),
            new("Contabilidad Básica"),
            new("Contabilidad Comercial"),
            new("Orientación profesional"),
            new("Proyecto de Investigación")
        };

        await db.Subjects.AddRangeAsync(subjects, ct);
        await db.SaveChangesAsync(ct);

        // =========================================================
        // 2) TEACHERS (cobertura suficiente)
        // =========================================================
        var teachers = new List<Teacher>
        {
            new("Ana Gómez",        new[] { "Lengua castellana", "Plan Lector", "Pre-escritura" }),
            new("Carlos Ruiz",      new[] { "Matemáticas", "Aritmética", "Geometría", "Álgebra" }),
            new("María Torres",     new[] { "Inglés" }),
            new("Luis Rojas",       new[] { "Ciencias naturales", "Biología", "Química" }),
            new("Sofía Herrera",    new[] { "Física", "Matemáticas" }),
            new("Jorge Martínez",   new[] { "Sociales", "Historia", "Geografía", "Competencia ciudadana" }),
            new("Camila Torres",    new[] { "Ética y valores", "Religión", "Cívica y Urbanidad (acompañamiento)" }),
            new("Paula Salazar",    new[] { "Artística", "Música" }),
            new("Ricardo López",    new[] { "Educación física y deportes" }),
            new("Laura Pérez",      new[] { "Informática" }),
            new("Julio Gómez",      new[] { "Filosofía", "Ciencias económicas y políticas", "Ética empresarial" }),
            new("Andrea Núñez",     new[] { "Emprendimiento", "Proyecto de Investigación", "Orientación profesional" }),
            new("Diana Romero",     new[] { "Contabilidad Básica", "Contabilidad Comercial" }),
        };
        await db.Teachers.AddRangeAsync(teachers, ct);
        await db.SaveChangesAsync(ct);

        // =========================================================
        // 3) ROOMS (aulas por grado + labs + informática + deportes)
        // Tags: "aula", "lab", "computer", "deportes"
        // =========================================================
        var rooms = new List<Room>
        {
            // Aulas de Preescolar – Primaria
            new("PRE-A", 28, new[] { "aula" }),
            new("PRE-B", 28, new[] { "aula" }),
            new("G1-A",  30, new[] { "aula" }),
            new("G1-B",  30, new[] { "aula" }),
            new("G2-A",  32, new[] { "aula" }),
            new("G2-B",  32, new[] { "aula" }),
            new("G3-A",  32, new[] { "aula" }),
            new("G3-B",  32, new[] { "aula" }),
            new("G4-A",  34, new[] { "aula" }),
            new("G4-B",  34, new[] { "aula" }),
            new("G5-A",  34, new[] { "aula" }),
            new("G5-B",  34, new[] { "aula" }),

            // Aulas de Secundaria
            new("G6-A",  35, new[] { "aula" }),
            new("G6-B",  35, new[] { "aula" }),
            new("G7-A",  35, new[] { "aula" }),
            new("G7-B",  35, new[] { "aula" }),
            new("G8-A",  35, new[] { "aula" }),
            new("G8-B",  35, new[] { "aula" }),
            new("G9-A",  36, new[] { "aula" }),
            new("G9-B",  36, new[] { "aula" }),
            new("G10-A", 36, new[] { "aula" }),
            new("G10-B", 36, new[] { "aula" }),
            new("G11-A", 36, new[] { "aula" }),
            new("G11-B", 36, new[] { "aula" }),

            // Especiales
            new("LAB-CIENCIAS", 28, new[] { "lab" }),
            new("LAB-INFO",     28, new[] { "computer" }),  // si tu scheduler busca "computer" para Informática
            new("CANCHA",       60, new[] { "deportes" }),
            new("AULA-MUSICA",  25, new[] { "aula" })
        };
        await db.Rooms.AddRangeAsync(rooms, ct);
        await db.SaveChangesAsync(ct);

        // =========================================================
        // 4) GROUPS (Pre, 1..11; secciones A y B)
        // =========================================================
        var grades = new List<string> { "Pre" };
        grades.AddRange(Enumerable.Range(1, 11).Select(i => i.ToString()));

        var groupList = new List<Group>();
        var rnd = new Random(42);

        foreach (var gr in grades)
        {
            // Tamaño aleatorio razonable por grupo
            var sizeA = gr == "Pre" ? rnd.Next(24, 30) : rnd.Next(28, 36);
            var sizeB = gr == "Pre" ? rnd.Next(22, 28) : rnd.Next(26, 34);

            groupList.Add(new Group(gr, "A", sizeA));
            groupList.Add(new Group(gr, "B", sizeB));
        }
        await db.Groups.AddRangeAsync(groupList, ct);
        await db.SaveChangesAsync(ct);

        // =========================================================
        // 5) CURRICULUM (IH por grado)
        // Malla razonable (MVP) basada en prácticas comunes.
        // Puedes ajustar IH según tu tabla real.
        // =========================================================

        // Helper para crear líneas
        List<StudyPlanEntry> MakeIH(int year, string grade, Dictionary<string, int> map)
            => map.Select(kv => new StudyPlanEntry(year, grade, kv.Key, kv.Value)).ToList();

        var ih = new List<StudyPlanEntry>();

        // Preescolar (enfoque básico + artística + EF)
        var ihPre = new Dictionary<string, int>
        {
            ["Pre-escritura"] = 4,
            ["Lengua castellana"] = 3,
            ["Matemáticas"] = 3,
            ["Inglés"] = 1,
            ["Artística"] = 1,
            ["Música"] = 1,
            ["Educación física y deportes"] = 1,
            ["Plan Lector"] = 1,
            ["Cívica y Urbanidad (acompañamiento)"] = 1
        };
        ih.AddRange(MakeIH(2025, "Pre", ihPre));

        // Primaria (1°–5°)
        Dictionary<string, int> BasePrimaria(int grado) => new()
        {
            ["Lengua castellana"] = 5,
            ["Matemáticas"] = 5,
            ["Inglés"] = grado >= 3 ? 3 : 2,
            ["Ciencias naturales"] = grado >= 3 ? 3 : 2,
            ["Sociales"] = grado >= 3 ? 2 : 1,
            ["Artística"] = 1,
            ["Música"] = grado >= 3 ? 1 : 0,
            ["Ética y valores"] = 1,
            ["Religión"] = 1,
            ["Educación física y deportes"] = 2,
            ["Plan Lector"] = 1
        };

        for (int g = 1; g <= 5; g++)
        {
            var map = BasePrimaria(g).Where(kv => kv.Value > 0).ToDictionary(k => k.Key, v => v.Value);
            ih.AddRange(MakeIH(2025, g.ToString(), map));
        }

        // Secundaria baja (6°–9°)
        Dictionary<string, int> BaseSecundariaBaja(int grado) => new()
        {
            ["Lengua castellana"] = 5,
            ["Inglés"] = 3,
            ["Aritmética"] = grado == 6 ? 3 : 0,
            ["Geometría"] = grado >= 7 ? 2 : 0,
            ["Álgebra"] = grado >= 8 ? 2 : 0,
            ["Ciencias naturales"] = grado == 6 ? 3 : 0,
            ["Biología"] = grado == 7 ? 2 : 0,
            ["Química"] = grado == 8 ? 2 : 0,
            ["Física"] = grado == 9 ? 2 : 0,
            ["Sociales"] = 2,
            ["Historia"] = grado >= 8 ? 2 : 0,
            ["Geografía"] = grado >= 8 ? 2 : 0,
            ["Competencia ciudadana"] = grado >= 7 ? 1 : 0,
            ["Informática"] = 2,
            ["Artística"] = 1,
            ["Música"] = 1,
            ["Ética y valores"] = 1,
            ["Religión"] = 1,
            ["Educación física y deportes"] = 2,
            ["Plan Lector"] = 1
        };

        for (int g = 6; g <= 9; g++)
        {
            var map = BaseSecundariaBaja(g).Where(kv => kv.Value > 0).ToDictionary(k => k.Key, v => v.Value);
            ih.AddRange(MakeIH(2025, g.ToString(), map));
        }

        // Media (10°–11°)
        Dictionary<string, int> BaseMedia(int grado) => new()
        {
            ["Lengua castellana"] = 4,
            ["Inglés"] = 4,
            ["Álgebra"] = 2,
            ["Trigonometría"] = grado == 10 ? 2 : 0,
            ["Cálculo"] = grado == 11 ? 2 : 0,
            ["Biología"] = 0, // puedes subir a 2 si tu colegio lo exige
            ["Química"] = 2,
            ["Física"] = 2,
            ["Filosofía"] = 2,
            ["Ciencias económicas y políticas"] = grado >= 10 ? 1 : 0,
            ["Ética y valores"] = 1,
            ["Ética empresarial"] = grado == 11 ? 1 : 0,
            ["Religión"] = 1,
            ["Informática"] = 2,
            ["Emprendimiento"] = grado >= 10 ? 2 : 0,
            ["Contabilidad Básica"] = grado == 10 ? 2 : 0,
            ["Contabilidad Comercial"] = grado == 11 ? 2 : 0,
            ["Proyecto de Investigación"] = 2,
            ["Educación física y deportes"] = 2,
            ["Artística"] = 1,
            ["Música"] = 1,
            ["Competencia ciudadana"] = 1,
            ["Plan Lector"] = 1
        };

        foreach (var g in new[] { 10, 11 })
        {
            var map = BaseMedia(g).Where(kv => kv.Value > 0).ToDictionary(k => k.Key, v => v.Value);
            ih.AddRange(MakeIH(2025, g.ToString(), map));
        }

        await db.CurriculumLines.AddRangeAsync(ih, ct);
        await db.SaveChangesAsync(ct);
    }
}
