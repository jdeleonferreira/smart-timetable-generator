// Arma la cuadrícula de un horario: filas = franjas (hora de clase), columnas = días o cursos.

export type Weekday = 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday';

export const WEEKDAYS: Weekday[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

export const DAY_NAMES: Record<Weekday, string> = {
  Monday: 'Lunes',
  Tuesday: 'Martes',
  Wednesday: 'Miércoles',
  Thursday: 'Jueves',
  Friday: 'Viernes',
  Saturday: 'Sábado',
  Sunday: 'Domingo'
};

/** Lo que necesita la cuadrícula de cada clase. */
export interface GridLesson {
  id: string;
  day: Weekday;
  periodNumber: number;
  start?: string;
  end?: string;
  shiftName: string;
  courseId: string;
  courseName: string;
  subjectName: string;
  subjectCode?: string | null;
  teacherName?: string | null;
  spaceName?: string | null;
}

export interface GridRow {
  key: string;
  shiftName: string;
  periodNumber: number;
  /** "7:00–7:45" si se conoce la hora. */
  time?: string;
  /** Clases por columna (clave de columna → clases). */
  cells: Record<string, GridLesson[]>;
}

export interface GridColumn {
  key: string;
  label: string;
}

export interface WeekGrid {
  columns: GridColumn[];
  rows: GridRow[];
}

const dayOrder = (day: Weekday) => WEEKDAYS.indexOf(day);

function formatTime(value?: string): string | undefined {
  if (!value) return undefined;
  const [h, m] = value.split(':');
  return `${Number(h)}:${m}`;
}

/**
 * Vista semanal (por curso o por docente): una columna por día con clases (lunes a viernes siempre),
 * una fila por jornada y franja, en orden.
 */
export function buildWeekGrid(lessons: GridLesson[]): WeekGrid {
  const days = new Set<Weekday>(['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']);
  lessons.forEach((l) => days.add(l.day));
  const columns = [...days].sort((a, b) => dayOrder(a) - dayOrder(b)).map((d) => ({ key: d, label: DAY_NAMES[d] }));
  return { columns, rows: buildRows(lessons, (l) => l.day) };
}

/**
 * Vista institucional de un día: una columna por curso (en el orden recibido), una fila por jornada y franja.
 */
export function buildDayGrid(lessons: GridLesson[], courses: GridColumn[]): WeekGrid {
  return { columns: courses, rows: buildRows(lessons, (l) => l.courseId) };
}

function buildRows(lessons: GridLesson[], columnOf: (lesson: GridLesson) => string): GridRow[] {
  const rows = new Map<string, GridRow>();
  for (const lesson of lessons) {
    const key = `${lesson.shiftName}#${lesson.periodNumber}`;
    let row = rows.get(key);
    if (!row) {
      row = { key, shiftName: lesson.shiftName, periodNumber: lesson.periodNumber, cells: {} };
      rows.set(key, row);
    }
    if (!row.time && lesson.start && lesson.end) row.time = `${formatTime(lesson.start)}–${formatTime(lesson.end)}`;
    (row.cells[columnOf(lesson)] ??= []).push(lesson);
  }

  // Las jornadas en el orden de su primera hora; dentro de cada jornada, por franja
  const firstStart = new Map<string, number>();
  for (const lesson of lessons) {
    const minutes = toMinutes(lesson.start);
    const current = firstStart.get(lesson.shiftName) ?? Number.MAX_SAFE_INTEGER;
    if (minutes < current) firstStart.set(lesson.shiftName, minutes);
  }

  return [...rows.values()].sort((a, b) => {
    if (a.shiftName !== b.shiftName) {
      const sa = firstStart.get(a.shiftName) ?? Number.MAX_SAFE_INTEGER;
      const sb = firstStart.get(b.shiftName) ?? Number.MAX_SAFE_INTEGER;
      return sa !== sb ? sa - sb : a.shiftName.localeCompare(b.shiftName);
    }
    return a.periodNumber - b.periodNumber;
  });
}

function toMinutes(value?: string): number {
  if (!value) return Number.MAX_SAFE_INTEGER;
  const [h, m] = value.split(':').map(Number);
  return h * 60 + m;
}

/** Color estable por asignatura, para distinguirlas en la cuadrícula. */
const PALETTE = ['blue', 'teal', 'grape', 'orange', 'cyan', 'pink', 'lime', 'indigo', 'yellow', 'red', 'green', 'violet'];

export function subjectColor(subjectName: string): string {
  let hash = 0;
  for (const ch of subjectName) hash = (hash * 31 + ch.charCodeAt(0)) >>> 0;
  return PALETTE[hash % PALETTE.length];
}
