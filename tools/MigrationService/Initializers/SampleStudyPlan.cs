namespace MigrationService.Initializers;

/// <summary>
/// Plan de estudios de ejemplo, transcrito del documento "PLAN DE ESTUDIOS" (Consejo Académico):
/// Preescolar, Básica y Media Técnica con especialidad Comercial.
/// </summary>
internal static class SampleStudyPlan
{
    /// <summary>Marca de asignatura transversal («*» en el documento).</summary>
    public const int T = -1;

    /// <summary>Columnas de intensidad horaria: Preescolar, 1º … 11º.</summary>
    public static readonly (string Name, string ShortName, int Order)[] Grades =
    [
        ("Preescolar", "PRE", 0),
        ("Primero", "1º", 1),
        ("Segundo", "2º", 2),
        ("Tercero", "3º", 3),
        ("Cuarto", "4º", 4),
        ("Quinto", "5º", 5),
        ("Sexto", "6º", 6),
        ("Séptimo", "7º", 7),
        ("Octavo", "8º", 8),
        ("Noveno", "9º", 9),
        ("Décimo", "10º", 10),
        ("Undécimo", "11º", 11)
    ];

    public sealed record SubjectRow(string Name, string Code, int?[] Hours, string? Note = null, string? IntegratedInto = null);

    public sealed record AreaRow(string Name, SubjectRow[] Subjects);

    // Valores: null = no se dicta en el grado, T = transversal («*»), n = horas semanales.
    //                                                       PRE   1º    2º    3º    4º    5º    6º    7º    8º    9º    10º   11º
    public static readonly AreaRow[] Areas =
    [
        new("Ciencias Naturales y Educación Ambiental",
        [
            new("Biología", "BIO",                    [2,    5,    5,    5,    5,    5,    5,    5,    5,    4,    null, null]),
            new("Química", "QUI",                     [null, null, null, null, null, null, null, null, null, null, 4,    4]),
            new("Física", "FIS",                      [null, null, null, null, null, null, null, null, null, 1,    4,    4])
        ]),
        new("Ciencias Sociales",
        [
            new("Sociales", "SOC",                    [2,    4,    4,    4,    null, null, null, null, null, null, 1,    1]),
            new("Historia", "HIS",                    [null, null, null, null, 2,    2,    2,    2,    2,    2,    null, null]),
            new("Geografía", "GEO",                   [null, null, null, null, 2,    2,    2,    2,    2,    2,    null, null]),
            new("Competencia ciudadana", "CCI",       [null, T,    T,    T,    T,    T,    1,    1,    1,    1,    null, null],
                "Se trabaja como eje transversal en el desarrollo de los contenidos de las Ciencias Sociales (1° a 5°).",
                IntegratedInto: "Sociales")
        ]),
        new("Filosofía",
        [
            new("Filosofía", "FIL",                   [null, null, null, null, null, null, null, null, 1,    1,    2,    2])
        ]),
        new("Ciencias Económicas y Políticas",
        [
            new("Ciencias económicas y políticas", "CEP", [null, null, null, null, null, null, null, null, null, null, 1, 1])
        ]),
        new("Educación Artística y Cultural",
        [
            new("Artística", "ART",                   [1,    1,    1,    1,    1,    1,    null, null, null, null, null, null]),
            new("Música", "MUS",                      [null, null, null, null, null, null, 1,    1,    1,    1,    1,    1])
        ]),
        new("Educación Ética y en Valores Humanos",
        [
            new("Ética y valores", "ETI",             [null, 1,    1,    1,    1,    1,    1,    1,    1,    1,    null, null]),
            new("Ética empresarial", "ETE",           [null, null, null, null, null, null, null, null, null, null, 1,    1]),
            new("Cívica y Urbanidad (acompañamiento)", "CIV", [1, 1,  1,    1,    1,    1,    1,    1,    1,    1,    1,    1])
        ]),
        new("Educación Física, Recreación y Deportes",
        [
            new("Educación física y deportes", "EDF", [1,    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,    2])
        ]),
        new("Educación Religiosa",
        [
            new("Religión", "REL",                    [1,    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,    2])
        ]),
        new("Humanidades",
        [
            new("Lengua castellana", "LEN",           [null, 5,    5,    5,    5,    5,    5,    5,    5,    5,    4,    4]),
            new("Inglés", "ING",                      [1,    3,    3,    3,    3,    3,    4,    4,    4,    4,    4,    4]),
            new("Pre-escritura", "PES",               [4,    null, null, null, null, null, null, null, null, null, null, null]),
            new("Plan Lector", "PLE",                 [1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1],
                "Se desarrolla semanalmente durante 45 minutos en el horario de clases los días miércoles.")
        ]),
        new("Matemáticas",
        [
            new("Aritmética", "ARI",                  [null, 5,    5,    5,    5,    5,    6,    6,    null, null, null, null]),
            new("Geometría", "GEM",                   [null, 1,    1,    1,    1,    1,    1,    1,    1,    1,    null, null]),
            new("Álgebra", "ALG",                     [null, null, null, null, null, null, null, null, 4,    4,    null, null]),
            new("Trigonometría", "TRI",               [null, null, null, null, null, null, null, null, null, null, 4,    null]),
            new("Cálculo", "CAL",                     [null, null, null, null, null, null, null, null, null, null, null, 4]),
            new("Pre-matemáticas", "PMA",             [5,    null, null, null, null, null, null, null, null, null, null, null])
        ]),
        new("Tecnología e Informática",
        [
            new("Informática", "INF",                 [1,    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,    2])
        ]),
        new("Áreas y Asignaturas Optativas",
        [
            new("Emprendimiento", "EMP",              [null, null, null, null, null, null, null, null, 1,    1,    1,    null]),
            new("Contabilidad básica", "COB",         [null, null, null, null, null, null, 2,    2,    2,    null, null, null]),
            new("Contabilidad comercial", "COC",      [null, null, null, null, null, null, null, null, null, 2,    3,    3]),
            new("Orientación profesional", "ORP",     [null, null, null, null, null, null, null, null, null, null, null, 1]),
            new("Proyecto de investigación", "PIN",   [null, null, null, null, null, null, null, null, null, T,    T,    T],
                "Se desarrolla en horas de la tarde (el coordinador puede configurarlo como contrajornada).")
        ])
    ];

    /// <summary>Totales semanales del documento, para verificar la transcripción.</summary>
    public static readonly int[] ExpectedWeeklyTotals = [20, 33, 33, 33, 33, 33, 38, 38, 38, 38, 38, 38];

    public const string PlanNotes =
        "* Las Competencias Ciudadanas se trabajan como eje transversal en el desarrollo de los contenidos de las Ciencias Sociales (1° a 5°).\n" +
        "* La asignatura de Proyecto de Investigación, de noveno a undécimo, se desarrolla en horas de la tarde.\n" +
        "Total semanal: 8 horas de 45 minutos.";

    public static readonly (string Name, bool Mandatory)[] TrainingProjects =
    [
        ("Festival de porras", false),
        ("Festival intercolegial de danzas", false),
        ("Plan lector", false),
        ("Formación sexual", true),
        ("Educación para la democracia", true),
        ("Utilización del tiempo libre", true),
        ("Prevención de desastres y educación ambiental", true)
    ];
}
