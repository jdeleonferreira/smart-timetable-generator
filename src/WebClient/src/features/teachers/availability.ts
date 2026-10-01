// Lógica de la cuadrícula de disponibilidad de un docente (sin interfaz, para poder probarla).
import { TimeOnly } from '@microsoft/kiota-abstractions';
import type { AvailabilityRuleDto, CampusDto } from '../../api/client';
import { WEEKDAYS, type Weekday } from '../timetable/weekGrid';

export type Mark = 'Unavailable' | 'Avoid';

export interface AvailabilityRow {
  /** Identifica la hora de clase: jornada + número de franja. */
  key: string;
  shiftId: string;
  shiftName: string;
  number: number;
  startMin: number;
  endMin: number;
  /** Días en que esa jornada tiene clase. */
  days: Weekday[];
}

export interface AvailabilityLayout {
  days: Weekday[];
  rows: AvailabilityRow[];
}

/** Minutos desde la medianoche de un valor de hora de la API. */
export function toMinutes(time?: TimeOnly | null): number {
  return time ? time.hours * 60 + time.minutes : 0;
}

export function formatMinutes(total: number): string {
  const h = Math.floor(total / 60);
  const m = total % 60;
  return `${h}:${String(m).padStart(2, '0')}`;
}

export function timeOf(totalMinutes: number): TimeOnly {
  return new TimeOnly({ hours: Math.floor(totalMinutes / 60), minutes: totalMinutes % 60 });
}

/** Filas (horas de clase de cada jornada de la sede, por orden de hora) y días de la semana de la cuadrícula. */
export function layoutFor(campus: CampusDto | undefined): AvailabilityLayout {
  const rows: AvailabilityRow[] = [];
  const days = new Set<Weekday>();

  for (const shift of campus?.shifts ?? []) {
    const shiftDays = (shift.workDays ?? []) as Weekday[];
    shiftDays.forEach((d) => days.add(d));

    for (const block of shift.classBlocks ?? []) {
      rows.push({
        key: `${shift.id}-${block.number}`,
        shiftId: shift.id!,
        shiftName: shift.name ?? '',
        number: block.number ?? 0,
        startMin: toMinutes(block.start),
        endMin: toMinutes(block.end),
        days: shiftDays
      });
    }
  }

  rows.sort((a, b) => a.startMin - b.startMin);
  return { days: WEEKDAYS.filter((d) => days.has(d)), rows };
}

export const cellKey = (day: Weekday, row: AvailabilityRow) => `${day}|${row.key}`;

const overlaps = (aStart: number, aEnd: number, bStart: number, bEnd: number) => aStart < bEnd && bStart < aEnd;

/** Marcas de la cuadrícula a partir de las reglas guardadas (una regla marca cada hora de clase que toca). */
export function marksFromRules(rules: AvailabilityRuleDto[], layout: AvailabilityLayout): Map<string, Mark> {
  const marks = new Map<string, Mark>();
  for (const rule of rules) {
    if (!rule.day || !rule.kind || (rule.kind !== 'Unavailable' && rule.kind !== 'Avoid')) continue;
    const start = toMinutes(rule.start);
    const end = toMinutes(rule.end);

    for (const row of layout.rows) {
      if (!row.days.includes(rule.day as Weekday)) continue;
      if (!overlaps(start, end, row.startMin, row.endMin)) continue;

      const key = cellKey(rule.day as Weekday, row);
      // Si hay solapamiento, "no puede" manda sobre "prefiere evitar"
      if (marks.get(key) !== 'Unavailable') marks.set(key, rule.kind);
    }
  }
  return marks;
}

/**
 * Reglas a guardar: las marcas de la cuadrícula (horas seguidas con la misma marca se unen en una regla)
 * más las reglas existentes que no tocan ninguna hora de esta cuadrícula (p. ej. horas de otra sede).
 */
export function rulesFromMarks(
  marks: Map<string, Mark>,
  layout: AvailabilityLayout,
  existing: AvailabilityRuleDto[]
): AvailabilityRuleDto[] {
  const kept = existing.filter((rule) => {
    const start = toMinutes(rule.start);
    const end = toMinutes(rule.end);
    return !layout.rows.some((row) => row.days.includes(rule.day as Weekday) && overlaps(start, end, row.startMin, row.endMin));
  });

  const created: AvailabilityRuleDto[] = [];
  for (const day of layout.days) {
    let open: { kind: Mark; startMin: number; endMin: number } | null = null;
    const flush = () => {
      if (open) created.push({ day, kind: open.kind, start: timeOf(open.startMin), end: timeOf(open.endMin) });
      open = null;
    };

    for (const row of layout.rows.filter((r) => r.days.includes(day))) {
      const mark = marks.get(cellKey(day, row));
      if (!mark) {
        flush();
        continue;
      }
      // Une horas consecutivas (sin descanso en medio) con la misma marca
      if (open && open.kind === mark && open.endMin === row.startMin) {
        open.endMin = row.endMin;
      } else {
        flush();
        open = { kind: mark, startMin: row.startMin, endMin: row.endMin };
      }
    }
    flush();
  }

  return [...kept, ...created];
}

/** Aplica el pincel a una lista de celdas: si todas ya lo tienen, las limpia; si no, las marca todas. */
export function paint(marks: Map<string, Mark>, keys: string[], brush: Mark | null): Map<string, Mark> {
  const next = new Map(marks);
  const allHaveIt = brush !== null && keys.length > 0 && keys.every((k) => next.get(k) === brush);
  for (const key of keys) {
    if (brush === null || allHaveIt) next.delete(key);
    else next.set(key, brush);
  }
  return next;
}

export function sameMarks(a: Map<string, Mark>, b: Map<string, Mark>): boolean {
  if (a.size !== b.size) return false;
  for (const [k, v] of a) if (b.get(k) !== v) return false;
  return true;
}
