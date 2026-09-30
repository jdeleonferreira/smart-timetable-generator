import { buildDayGrid, buildWeekGrid, subjectColor, type GridLesson } from './weekGrid';

const lesson = (overrides: Partial<GridLesson>): GridLesson => ({
  id: Math.random().toString(36),
  day: 'Monday',
  periodNumber: 1,
  shiftName: 'Mañana',
  courseId: 'c1',
  courseName: '6ºA',
  subjectName: 'Matemáticas',
  ...overrides
});

describe('buildWeekGrid', () => {
  it('siempre muestra de lunes a viernes y agrega el sábado solo si hay clases', () => {
    expect(buildWeekGrid([]).columns.map((c) => c.label)).toEqual(['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes']);
    expect(buildWeekGrid([lesson({ day: 'Saturday' })]).columns.map((c) => c.key)).toEqual([
      'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'
    ]);
  });

  it('ordena las filas por franja y ubica cada clase en su día', () => {
    const grid = buildWeekGrid([
      lesson({ day: 'Wednesday', periodNumber: 3, start: '09:15:00', end: '10:00:00', subjectName: 'Inglés' }),
      lesson({ day: 'Monday', periodNumber: 1, start: '07:00:00', end: '07:45:00' }),
      lesson({ day: 'Friday', periodNumber: 1, subjectName: 'Biología' })
    ]);

    expect(grid.rows.map((r) => r.periodNumber)).toEqual([1, 3]);
    expect(grid.rows[0].time).toBe('7:00–7:45');
    expect(grid.rows[0].cells.Monday.map((l) => l.subjectName)).toEqual(['Matemáticas']);
    expect(grid.rows[0].cells.Friday.map((l) => l.subjectName)).toEqual(['Biología']);
    expect(grid.rows[1].cells.Wednesday.map((l) => l.subjectName)).toEqual(['Inglés']);
    expect(grid.rows[1].cells.Monday).toBeUndefined();
  });

  it('separa las jornadas y pone primero la que empieza más temprano', () => {
    const grid = buildWeekGrid([
      lesson({ shiftName: 'Tarde', periodNumber: 1, start: '13:00:00', end: '13:45:00' }),
      lesson({ shiftName: 'Mañana', periodNumber: 2, start: '07:45:00', end: '08:30:00' }),
      lesson({ shiftName: 'Mañana', periodNumber: 1, start: '07:00:00', end: '07:45:00' })
    ]);

    expect(grid.rows.map((r) => `${r.shiftName} ${r.periodNumber}`)).toEqual(['Mañana 1', 'Mañana 2', 'Tarde 1']);
  });

  it('agrupa varias clases en la misma celda (docente con dos grupos a la vez no debería ocurrir, pero se muestra)', () => {
    const grid = buildWeekGrid([lesson({ courseName: '6ºA' }), lesson({ courseName: '6ºB' })]);
    expect(grid.rows[0].cells.Monday).toHaveLength(2);
  });
});

describe('buildDayGrid', () => {
  it('pone una columna por curso en el orden dado', () => {
    const courses = [
      { key: 'c1', label: '6ºA' },
      { key: 'c2', label: '6ºB' }
    ];
    const grid = buildDayGrid(
      [lesson({ courseId: 'c2', subjectName: 'Inglés' }), lesson({ courseId: 'c1', periodNumber: 2 })],
      courses
    );

    expect(grid.columns).toEqual(courses);
    expect(grid.rows[0].cells.c2.map((l) => l.subjectName)).toEqual(['Inglés']);
    expect(grid.rows[1].cells.c1).toHaveLength(1);
  });
});

describe('subjectColor', () => {
  it('da siempre el mismo color a la misma asignatura', () => {
    expect(subjectColor('Matemáticas')).toBe(subjectColor('Matemáticas'));
  });
});
