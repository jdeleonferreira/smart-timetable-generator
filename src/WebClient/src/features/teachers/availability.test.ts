import { TimeOnly } from '@microsoft/kiota-abstractions';
import { describe, expect, it } from 'vitest';
import type { AvailabilityRuleDto, CampusDto } from '../../api/client';
import { cellKey, layoutFor, marksFromRules, paint, rulesFromMarks, toMinutes, type Mark } from './availability';

const t = (h: number, m = 0) => new TimeOnly({ hours: h, minutes: m });

// Jornada de la mañana de lunes a viernes: 7:00-7:50, 7:50-8:40, (descanso), 9:10-10:00
const campus: CampusDto = {
  id: 'campus',
  name: 'Sede Principal',
  shifts: [
    {
      id: 'morning',
      name: 'Mañana',
      workDays: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'],
      classBlocks: [
        { number: 1, start: t(7), end: t(7, 50) },
        { number: 2, start: t(7, 50), end: t(8, 40) },
        { number: 3, start: t(9, 10), end: t(10) }
      ]
    }
  ]
} as CampusDto;

const layout = layoutFor(campus);
const rule = (day: AvailabilityRuleDto['day'], start: TimeOnly, end: TimeOnly, kind: Mark): AvailabilityRuleDto => ({ day, start, end, kind });

describe('layoutFor', () => {
  it('arma una fila por hora de clase y las columnas con los días de la jornada', () => {
    expect(layout.days).toEqual(['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']);
    expect(layout.rows.map((r) => [r.number, r.startMin, r.endMin])).toEqual([
      [1, 420, 470],
      [2, 470, 520],
      [3, 550, 600]
    ]);
  });

  it('no tiene filas si la sede no tiene jornadas', () => {
    expect(layoutFor(undefined).rows).toEqual([]);
  });
});

describe('marksFromRules', () => {
  it('marca cada hora de clase que toca la regla', () => {
    const marks = marksFromRules([rule('Monday', t(7), t(8, 40), 'Unavailable')], layout);

    expect(marks.get(cellKey('Monday', layout.rows[0]))).toBe('Unavailable');
    expect(marks.get(cellKey('Monday', layout.rows[1]))).toBe('Unavailable');
    expect(marks.has(cellKey('Monday', layout.rows[2]))).toBe(false);
    expect(marks.has(cellKey('Tuesday', layout.rows[0]))).toBe(false);
  });

  it('"no puede" manda sobre "prefiere evitar" si se solapan', () => {
    const marks = marksFromRules(
      [rule('Friday', t(7), t(7, 50), 'Avoid'), rule('Friday', t(7), t(7, 50), 'Unavailable')],
      layout
    );

    expect(marks.get(cellKey('Friday', layout.rows[0]))).toBe('Unavailable');
  });
});

describe('rulesFromMarks', () => {
  it('une las horas seguidas con la misma marca y separa las que tienen un descanso en medio', () => {
    const marks = new Map<string, Mark>(layout.rows.map((r) => [cellKey('Monday', r), 'Unavailable'] as [string, Mark]));

    const rules = rulesFromMarks(marks, layout, []);

    expect(rules.map((r) => [r.day, toMinutes(r.start), toMinutes(r.end), r.kind])).toEqual([
      ['Monday', 420, 520, 'Unavailable'],
      ['Monday', 550, 600, 'Unavailable']
    ]);
  });

  it('no une marcas distintas', () => {
    const marks = new Map<string, Mark>([
      [cellKey('Monday', layout.rows[0]), 'Unavailable'],
      [cellKey('Monday', layout.rows[1]), 'Avoid']
    ]);

    expect(rulesFromMarks(marks, layout, []).map((r) => r.kind)).toEqual(['Unavailable', 'Avoid']);
  });

  it('conserva las reglas que no tocan ninguna hora de esta cuadrícula (otra sede) y reemplaza las que sí', () => {
    const afternoon = rule('Monday', t(14), t(15), 'Unavailable');
    const old = rule('Monday', t(7), t(7, 50), 'Unavailable');
    const marks = new Map<string, Mark>([[cellKey('Tuesday', layout.rows[0]), 'Avoid']]);

    const rules = rulesFromMarks(marks, layout, [afternoon, old]);

    expect(rules).toContain(afternoon);
    expect(rules).not.toContain(old);
    expect(rules.filter((r) => r.day === 'Tuesday')).toHaveLength(1);
  });

  it('ida y vuelta: lo guardado vuelve a mostrarse igual', () => {
    const marks = new Map<string, Mark>([
      [cellKey('Monday', layout.rows[0]), 'Unavailable'],
      [cellKey('Monday', layout.rows[2]), 'Unavailable'],
      [cellKey('Friday', layout.rows[1]), 'Avoid']
    ]);

    const back = marksFromRules(rulesFromMarks(marks, layout, []), layout);

    expect([...back].sort()).toEqual([...marks].sort());
  });
});

describe('paint', () => {
  const keys = layout.rows.map((r) => cellKey('Monday', r));

  it('marca todas las celdas y, si ya estaban todas marcadas, las limpia', () => {
    const marked = paint(new Map(), keys, 'Unavailable');
    expect(marked.size).toBe(3);

    const cleared = paint(marked, keys, 'Unavailable');
    expect(cleared.size).toBe(0);
  });

  it('otra marca sobrescribe, y borrar limpia', () => {
    const marked = paint(new Map(), keys, 'Unavailable');

    expect([...paint(marked, keys, 'Avoid').values()]).toEqual(['Avoid', 'Avoid', 'Avoid']);
    expect(paint(marked, keys, null).size).toBe(0);
  });

  it('no modifica el mapa original', () => {
    const original = new Map<string, Mark>();
    paint(original, keys, 'Unavailable');
    expect(original.size).toBe(0);
  });
});
