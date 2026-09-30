import { Box, Paper, Table, Text } from '@mantine/core';
import { Fragment } from 'react';
import { subjectColor, type GridLesson, type WeekGrid } from './weekGrid';

export type GridMode = 'course' | 'teacher' | 'day';

function LessonBlock({ lesson, mode }: { lesson: GridLesson; mode: GridMode }) {
  const color = `var(--mantine-color-${subjectColor(lesson.subjectName)}-6)`;
  const detail =
    mode === 'teacher' ? lesson.courseName : mode === 'course' ? lesson.teacherName : lesson.teacherName?.split(' ')[0];
  return (
    <Box className="lesson-block" style={{ ['--block-color' as string]: color }} data-testid="lesson">
      <Text size="sm" fw={600} lh={1.2} title={lesson.subjectName}>
        {mode === 'day' ? (lesson.subjectCode ?? lesson.subjectName) : lesson.subjectName}
      </Text>
      {detail && (
        <Text size="xs" c="dimmed" lh={1.2}>
          {detail}
        </Text>
      )}
      {mode !== 'day' && lesson.spaceName && (
        <Text size="xs" c="dimmed" lh={1.2}>
          {lesson.spaceName}
        </Text>
      )}
    </Box>
  );
}

/** Cuadrícula de horario: filas = franjas (agrupadas por jornada), columnas = días o cursos. */
export function TimetableGrid({ grid, mode }: { grid: WeekGrid; mode: GridMode }) {
  if (grid.rows.length === 0)
    return (
      <Paper withBorder p="xl">
        <Text c="dimmed" ta="center">
          No hay clases para mostrar.
        </Text>
      </Paper>
    );

  const shifts = new Set(grid.rows.map((r) => r.shiftName));

  return (
    <Paper withBorder>
      <Table.ScrollContainer minWidth={Math.max(700, 110 * grid.columns.length)} maxHeight="calc(100vh - 260px)">
        <Table className="matrix" withColumnBorders verticalSpacing={4} data-testid="timetable-grid">
          <Table.Thead>
            <Table.Tr>
              <Table.Th className="sticky-col" w={110}>
                Hora
              </Table.Th>
              {grid.columns.map((c) => (
                <Table.Th key={c.key} ta="center">
                  {c.label}
                </Table.Th>
              ))}
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {grid.rows.map((row, index) => (
              <Fragment key={row.key}>
                {shifts.size > 1 && (index === 0 || grid.rows[index - 1].shiftName !== row.shiftName) && (
                  <Table.Tr bg="gray.0">
                    <Table.Td colSpan={grid.columns.length + 1} fw={700} c="blue.8">
                      Jornada {row.shiftName}
                    </Table.Td>
                  </Table.Tr>
                )}
                <Table.Tr>
                  <Table.Td className="sticky-col">
                    <Text size="sm" fw={600}>
                      {row.periodNumber}ª hora
                    </Text>
                    {row.time && (
                      <Text size="xs" c="dimmed">
                        {row.time}
                      </Text>
                    )}
                  </Table.Td>
                  {grid.columns.map((c) => (
                    <Table.Td key={c.key} valign="top">
                      {(row.cells[c.key] ?? []).map((lesson) => (
                        <LessonBlock key={lesson.id} lesson={lesson} mode={mode} />
                      ))}
                    </Table.Td>
                  ))}
                </Table.Tr>
              </Fragment>
            ))}
          </Table.Tbody>
        </Table>
      </Table.ScrollContainer>
    </Paper>
  );
}
