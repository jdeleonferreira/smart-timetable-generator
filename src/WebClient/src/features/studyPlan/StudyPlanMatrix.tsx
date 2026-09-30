import { Badge, Group, Paper, Table, Text, Tooltip } from '@mantine/core';
import { Fragment } from 'react';
import type { StudyPlanDto, StudyPlanGradeDto, StudyPlanItemDto, StudyPlanSubjectDto } from '../../api/client';

export interface CellTarget {
  subject: StudyPlanSubjectDto;
  grade: StudyPlanGradeDto;
  item?: StudyPlanItemDto;
}

interface Props {
  plan: StudyPlanDto;
  /** Periodo a mostrar; sin periodo se muestra la IH general. */
  periodId?: string;
  editable: boolean;
  onCellClick: (target: CellTarget) => void;
}

/** IH que aplica en el periodo (ajuste del periodo o la general). */
export function hoursFor(item: StudyPlanItemDto, periodId?: string): number {
  if (item.deliveryMode === 'Transversal') return 0;
  const adjusted = periodId ? item.periodHours?.find((p) => p.academicPeriodId === periodId) : undefined;
  return adjusted?.weeklyHours ?? item.weeklyHours ?? 0;
}

function subjectName(plan: StudyPlanDto, subjectId?: string | null) {
  return plan.areas?.flatMap((a) => a.subjects ?? []).find((s) => s.id === subjectId)?.name;
}

function Cell({ plan, item, periodId }: { plan: StudyPlanDto; item?: StudyPlanItemDto; periodId?: string }) {
  if (!item)
    return (
      <Text span c="gray.4">
        —
      </Text>
    );

  if (item.deliveryMode === 'Transversal')
    return (
      <Tooltip label={`Transversal: se trabaja en ${subjectName(plan, item.integratedIntoSubjectId) ?? 'otra asignatura'}`}>
        <Badge size="sm" variant="light" color="gray">
          T*
        </Badge>
      </Tooltip>
    );

  const hours = hoursFor(item, periodId);
  const adjusted = periodId !== undefined && item.periodHours?.some((p) => p.academicPeriodId === periodId);
  const shift = plan.shifts?.find((s) => s.id === item.targetShiftId)?.name;

  return (
    <Group gap={4} justify="center" wrap="nowrap">
      <Text span fw={600} c={adjusted ? 'orange.8' : undefined}>
        {hours}
      </Text>
      {item.deliveryMode === 'CounterShift' && (
        <Tooltip label={`Contrajornada${shift ? ` (${shift})` : ''}`}>
          <Badge size="xs" variant="light" color="violet">
            CJ
          </Badge>
        </Tooltip>
      )}
      {item.note && (
        <Tooltip label={item.note} multiline w={240}>
          <Text span size="xs" c="dimmed">
            *
          </Text>
        </Tooltip>
      )}
    </Group>
  );
}

/**
 * Plan de estudios con la forma del documento: áreas y asignaturas en filas, grados en columnas,
 * y los totales semanales por grado al pie.
 */
export function StudyPlanMatrix({ plan, periodId, editable, onCellClick }: Props) {
  const grades = plan.grades ?? [];
  const areas = plan.areas ?? [];

  const total = (grade: StudyPlanGradeDto, kind: 'weeklyTotal' | 'counterShiftTotal') => {
    if (!periodId) return grade[kind] ?? 0;
    return grade.periodTotals?.find((p) => p.academicPeriodId === periodId)?.[kind] ?? 0;
  };
  const hasCounterShift = grades.some((g) => total(g, 'counterShiftTotal') > 0);

  return (
    <Paper withBorder>
      <Table.ScrollContainer minWidth={900} maxHeight="calc(100vh - 240px)">
        <Table className="matrix" striped={false} highlightOnHover withColumnBorders verticalSpacing={6} fz="sm">
          <Table.Thead>
            <Table.Tr>
              <Table.Th className="sticky-col" miw={240}>
                Área / asignatura
              </Table.Th>
              {grades.map((g) => (
                <Table.Th key={g.id} ta="center" title={g.name ?? undefined}>
                  {g.shortName}
                </Table.Th>
              ))}
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {areas.map((area) => (
              <Fragment key={area.id}>
                <Table.Tr bg="gray.0">
                  <Table.Td className="sticky-col" fw={700} c="blue.8" bg="gray.0">
                    {area.name}
                  </Table.Td>
                  <Table.Td colSpan={grades.length} bg="gray.0" />
                </Table.Tr>
                {(area.subjects ?? []).map((subject) => (
                  <Table.Tr key={subject.id}>
                    <Table.Td className="sticky-col" pl="lg">
                      <Group gap={6} wrap="nowrap">
                        <Text size="sm" c={subject.isActive ? undefined : 'dimmed'}>
                          {subject.name}
                        </Text>
                        {!subject.isActive && (
                          <Badge size="xs" color="gray" variant="outline">
                            inactiva
                          </Badge>
                        )}
                      </Group>
                    </Table.Td>
                    {grades.map((grade) => {
                      const item = subject.items?.find((i) => i.gradeId === grade.id);
                      const canEdit = editable && (subject.isActive || !!item);
                      return (
                        <Table.Td
                          key={grade.id}
                          className={`plan-cell${canEdit ? ' editable' : ''}`}
                          data-testid={`cell-${subject.name}-${grade.shortName}`}
                          onClick={canEdit ? () => onCellClick({ subject, grade, item }) : undefined}
                        >
                          <Cell plan={plan} item={item} periodId={periodId} />
                        </Table.Td>
                      );
                    })}
                  </Table.Tr>
                ))}
              </Fragment>
            ))}
          </Table.Tbody>
          <Table.Tfoot>
            <Table.Tr bg="blue.0">
              <Table.Th className="sticky-col" bg="blue.0">
                Total semanal
              </Table.Th>
              {grades.map((g) => (
                <Table.Th key={g.id} ta="center" data-testid={`total-${g.shortName}`}>
                  {total(g, 'weeklyTotal')}
                </Table.Th>
              ))}
            </Table.Tr>
            {hasCounterShift && (
              <Table.Tr bg="violet.0">
                <Table.Th className="sticky-col" bg="violet.0">
                  En contrajornada
                </Table.Th>
                {grades.map((g) => (
                  <Table.Th key={g.id} ta="center">
                    {total(g, 'counterShiftTotal') || '—'}
                  </Table.Th>
                ))}
              </Table.Tr>
            )}
          </Table.Tfoot>
        </Table>
      </Table.ScrollContainer>
    </Paper>
  );
}
