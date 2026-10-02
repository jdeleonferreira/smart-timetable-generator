import { Alert, Badge, Box, Button, Group, SegmentedControl, Select, Stack, Table, Text, UnstyledButton } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconCircleOff, IconInfoCircle, IconMoodSmile, IconWaveSine } from '@tabler/icons-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { api, type CampusDto, type TeacherDto } from '../../api/client';
import { errorMessage } from '../../lib/errors';
import { DAY_NAMES, type Weekday } from '../timetable/weekGrid';
import {
  cellKey,
  formatMinutes,
  layoutFor,
  marksFromRules,
  paint,
  rulesFromMarks,
  sameMarks,
  type AvailabilityRow,
  type Mark
} from './availability';

type Brush = Mark | 'Clear';

const MARK_LABELS: Record<Mark, string> = { Unavailable: 'No puede', Avoid: 'Prefiere evitar' };

/**
 * Cuadrícula semanal de un docente: filas = horas de clase de la sede, columnas = días.
 * Se marca con ✕ lo que no puede (el generador siempre lo respeta) y con ~ lo que prefiere evitar
 * (el generador lo evita mientras sea posible).
 */
export function AvailabilityGrid({ teacher, campuses, editable }: { teacher: TeacherDto; campuses: CampusDto[]; editable: boolean }) {
  const teacherCampuses = campuses.filter((c) => (teacher.campusIds ?? []).includes(c.id!));
  const [campusId, setCampusId] = useState<string | null>(teacherCampuses[0]?.id ?? null);
  const campus = teacherCampuses.find((c) => c.id === campusId) ?? teacherCampuses[0];
  const layout = layoutFor(campus);

  const saved = marksFromRules(teacher.availability ?? [], layout);
  const [edited, setEdited] = useState<Map<string, Mark> | null>(null);
  const [brush, setBrush] = useState<Brush>('Unavailable');
  const marks = edited ?? saved;
  const dirty = edited !== null && !sameMarks(edited, saved);

  const queryClient = useQueryClient();
  const save = useMutation({
    mutationFn: () =>
      api.teachers.byTeacherId(teacher.id!).availability.put({ rules: rulesFromMarks(marks, layout, teacher.availability ?? []) }),
    onSuccess: () => {
      notifications.show({ color: 'green', message: 'Disponibilidad guardada. Se aplicará al generar los horarios.' });
      setEdited(null);
      queryClient.invalidateQueries({ queryKey: ['teachers'] });
    },
    onError: (e) => notifications.show({ color: 'red', title: 'No se pudo guardar', message: errorMessage(e) })
  });

  if (teacherCampuses.length === 0)
    return (
      <Alert color="yellow" icon={<IconInfoCircle size={18} />}>
        Asigne al docente al menos una sede para definir su disponibilidad.
      </Alert>
    );

  if (layout.rows.length === 0)
    return (
      <Alert color="yellow" icon={<IconInfoCircle size={18} />}>
        La sede {campus?.name} aún no tiene horas de clase configuradas en sus jornadas.
      </Alert>
    );

  const change = (keys: string[]) => {
    if (!editable) return;
    setEdited(paint(marks, keys, brush === 'Clear' ? null : brush));
  };

  const cellsOfRow = (row: AvailabilityRow) => row.days.map((d) => cellKey(d, row));
  const cellsOfDay = (day: Weekday) => layout.rows.filter((r) => r.days.includes(day)).map((r) => cellKey(day, r));

  const total = (kind: Mark) => [...marks.values()].filter((m) => m === kind).length;
  const multipleShifts = new Set(layout.rows.map((r) => r.shiftId)).size > 1;

  return (
    <Stack gap="sm">
      <Group justify="space-between" align="flex-end">
        <Stack gap={4}>
          <Text size="sm" c="dimmed" maw={560}>
            {editable
              ? 'Elija una marca y pulse las horas. Pulse el nombre de un día o una hora para marcar toda la columna o fila.'
              : 'Horas en las que el docente no puede o prefiere no dictar clase.'}
          </Text>
          {editable && (
            <SegmentedControl
              size="xs"
              value={brush}
              onChange={(v) => setBrush(v as Brush)}
              data={[
                { value: 'Unavailable', label: '✕  No puede' },
                { value: 'Avoid', label: '~  Prefiere evitar' },
                { value: 'Clear', label: 'Borrar' }
              ]}
            />
          )}
        </Stack>
        {teacherCampuses.length > 1 && (
          <Select
            label="Sede"
            size="xs"
            w={200}
            allowDeselect={false}
            data={teacherCampuses.map((c) => ({ value: c.id!, label: c.name ?? '' }))}
            value={campus?.id ?? null}
            onChange={(v) => {
              setCampusId(v);
              setEdited(null);
            }}
          />
        )}
      </Group>

      <Table.ScrollContainer minWidth={520}>
        <Table withTableBorder withColumnBorders verticalSpacing={0} style={{ tableLayout: 'fixed' }}>
          <Table.Thead>
            <Table.Tr>
              <Table.Th w={140}>Hora</Table.Th>
              {layout.days.map((day) => (
                <Table.Th key={day} ta="center" p={0}>
                  <UnstyledButton
                    w="100%"
                    py={8}
                    disabled={!editable}
                    onClick={() => change(cellsOfDay(day))}
                    aria-label={`Marcar todo el ${DAY_NAMES[day]}`}
                    style={{ fontWeight: 600, cursor: editable ? 'pointer' : 'default' }}
                  >
                    {DAY_NAMES[day]}
                  </UnstyledButton>
                </Table.Th>
              ))}
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {layout.rows.map((row, index) => {
              const startsShift = multipleShifts && (index === 0 || layout.rows[index - 1].shiftId !== row.shiftId);
              return [
                startsShift && (
                  <Table.Tr key={`shift-${row.shiftId}`}>
                    <Table.Td colSpan={layout.days.length + 1} bg="navy.0" fw={700} fz="xs" py={4} c="navy.8">
                      Jornada {row.shiftName}
                    </Table.Td>
                  </Table.Tr>
                ),
                <Table.Tr key={row.key}>
                  <Table.Td p={0}>
                    <UnstyledButton
                      w="100%"
                      px="xs"
                      py={6}
                      disabled={!editable}
                      onClick={() => change(cellsOfRow(row))}
                      aria-label={`Marcar toda la ${row.number}ª hora`}
                      style={{ cursor: editable ? 'pointer' : 'default' }}
                    >
                      <Text size="sm" fw={600}>
                        {row.number}ª hora
                      </Text>
                      <Text size="xs" c="dimmed">
                        {formatMinutes(row.startMin)}–{formatMinutes(row.endMin)}
                      </Text>
                    </UnstyledButton>
                  </Table.Td>
                  {layout.days.map((day) => (
                    <Table.Td key={day} p={0} h={46}>
                      {row.days.includes(day) ? (
                        <Cell
                          mark={marks.get(cellKey(day, row))}
                          label={`${DAY_NAMES[day]}, ${formatMinutes(row.startMin)} a ${formatMinutes(row.endMin)}`}
                          testId={`avail-${day}-${row.key}`}
                          editable={editable}
                          onClick={() => change([cellKey(day, row)])}
                        />
                      ) : (
                        <Box bg="gray.1" h="100%" />
                      )}
                    </Table.Td>
                  ))}
                </Table.Tr>
              ];
            })}
          </Table.Tbody>
        </Table>
      </Table.ScrollContainer>

      <Group justify="space-between">
        <Group gap="xs">
          <Badge color="red" variant="light" leftSection={<IconCircleOff size={12} />}>
            No puede: {total('Unavailable')}
          </Badge>
          <Badge color="yellow" variant="light" leftSection={<IconWaveSine size={12} />}>
            Prefiere evitar: {total('Avoid')}
          </Badge>
          <Badge color="gray" variant="light" leftSection={<IconMoodSmile size={12} />}>
            Libres: {layout.rows.reduce((n, r) => n + r.days.length, 0) - marks.size}
          </Badge>
        </Group>
        {editable && (
          <Group gap="xs">
            <Button variant="default" size="xs" disabled={!dirty} onClick={() => setEdited(null)}>
              Descartar
            </Button>
            <Button size="xs" disabled={!dirty} loading={save.isPending} onClick={() => save.mutate()}>
              Guardar disponibilidad
            </Button>
          </Group>
        )}
      </Group>
    </Stack>
  );
}

function Cell({
  mark,
  label,
  testId,
  editable,
  onClick
}: {
  mark?: Mark;
  label: string;
  testId: string;
  editable: boolean;
  onClick: () => void;
}) {
  const style =
    mark === 'Unavailable'
      ? { bg: 'var(--mantine-color-red-1)', color: 'var(--mantine-color-red-8)', symbol: '✕' }
      : mark === 'Avoid'
        ? { bg: 'var(--mantine-color-yellow-1)', color: 'var(--mantine-color-yellow-9)', symbol: '~' }
        : { bg: 'transparent', color: 'var(--mantine-color-gray-5)', symbol: '' };

  return (
    <UnstyledButton
      w="100%"
      h="100%"
      mih={46}
      disabled={!editable}
      onClick={onClick}
      data-testid={testId}
      aria-label={`${label}: ${mark ? MARK_LABELS[mark] : 'libre'}`}
      title={mark ? MARK_LABELS[mark] : 'Libre'}
      style={{
        background: style.bg,
        color: style.color,
        fontSize: 20,
        fontWeight: 700,
        textAlign: 'center',
        cursor: editable ? 'pointer' : 'default'
      }}
    >
      {style.symbol}
    </UnstyledButton>
  );
}
