import { Alert, Badge, Group, Paper, Select, Stack, Table, Text, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconBook2, IconInfoCircle } from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../auth/AuthContext';
import { api, type CourseDto, type StudyPlanDto, type TeacherDto, type TeachingAssignmentDto } from '../api/client';
import { EmptyState, ErrorAlert, Loading } from '../components/PageState';
import { useSchool } from '../context/SchoolContext';
import { errorMessage } from '../lib/errors';

interface Row {
  subjectId: string;
  subjectName: string;
  areaId: string;
  hours: number;
}

/** Docente de cada materia en cada curso. Se puede dejar sin docente y asignarlo después. */
export function AssignmentsPage() {
  const { year, campus, loading } = useSchool();
  const { canManageCampus } = useAuth();
  const queryClient = useQueryClient();
  const enabled = !!year?.id && !!campus?.id;

  const plans = useQuery({
    queryKey: ['study-plans', year?.id, campus?.id],
    queryFn: () => api.studyPlans.get({ queryParameters: { academicYearId: year!.id!, campusId: campus!.id! } }),
    enabled
  });
  const planId = plans.data?.[0]?.id ?? undefined;
  const plan = useQuery({
    queryKey: ['study-plan', planId],
    queryFn: () => api.studyPlans.byStudyPlanId(planId!).get(),
    enabled: !!planId
  });
  const courses = useQuery({
    queryKey: ['courses', year?.id, campus?.id],
    queryFn: () => api.catalog.courses.get({ queryParameters: { academicYearId: year!.id!, campusId: campus!.id! } }),
    enabled
  });
  const teachers = useQuery({ queryKey: ['teachers'], queryFn: () => api.catalog.teachers.get() });
  const assignments = useQuery({
    queryKey: ['teaching-assignments', year?.id, campus?.id],
    queryFn: () => api.teachingAssignments.get({ queryParameters: { academicYearId: year!.id!, campusId: campus!.id! } }),
    enabled
  });

  const assign = useMutation({
    mutationFn: ({ courseId, subjectId, teacherId }: { courseId: string; subjectId: string; teacherId: string | null }) =>
      api.teachingAssignments.courses.byCourseId(courseId).subjects.bySubjectId(subjectId).put({ teacherId }),
    onSuccess: (result) => {
      const updated = result?.updatedLessons ?? 0;
      const conflicts = result?.conflictingLessons ?? 0;
      notifications.show({
        color: conflicts > 0 ? 'yellow' : 'green',
        title: 'Docente asignado',
        message:
          conflicts > 0
            ? `${updated} clases actualizadas. ${conflicts} no se pudieron por cruce de horario del docente: vuelva a generar el horario.`
            : updated > 0
              ? `${updated} clases del horario en borrador se actualizaron.`
              : 'Se usará al generar el horario.'
      });
      queryClient.invalidateQueries({ queryKey: ['teaching-assignments'] });
      queryClient.invalidateQueries({ queryKey: ['lessons'] });
    },
    onError: (e) => notifications.show({ color: 'red', title: 'No se pudo asignar', message: errorMessage(e) })
  });

  if (loading || plans.isLoading || plan.isLoading || courses.isLoading || teachers.isLoading || assignments.isLoading)
    return <Loading label="Cargando la asignación de docentes…" />;
  const error = plans.error ?? plan.error ?? courses.error ?? teachers.error ?? assignments.error;
  if (error) return <ErrorAlert error={error} />;

  if (!plan.data)
    return (
      <EmptyState icon={IconBook2} title={`No hay plan de estudios ${year?.year ?? ''} para ${campus?.name ?? 'esta sede'}`}>
        <Text c="dimmed" size="sm">
          Cree el plan de estudios para ver las materias de cada curso.
        </Text>
      </EmptyState>
    );

  const editable = canManageCampus(campus?.id);
  const rowsByGrade = rowsOf(plan.data);
  const courseList = courses.data ?? [];

  return (
    <Stack gap="md">
      <div>
        <Title order={2}>Asignación de docentes</Title>
        <Text c="dimmed" size="sm">
          {campus?.name} · {year?.year}
        </Text>
      </div>

      <Alert icon={<IconInfoCircle size={18} />} color="navy" variant="light">
        Los docentes son opcionales: el horario se genera aunque una materia no tenga docente. Aquí puede asignarlo cuando lo tenga; las clases ya
        generadas en borrador se actualizan. Las materias sin docente fijado reciben una propuesta automática al generar.
      </Alert>

      {courseList.length === 0 && (
        <Text c="dimmed" size="sm">
          Esta sede no tiene cursos en {year?.year}.
        </Text>
      )}

      {courseList.map((course) => (
        <CourseAssignments
          key={course.id}
          course={course}
          rows={rowsByGrade.get(course.grade ?? '') ?? []}
          teachers={(teachers.data ?? []).filter((t) => t.isActive && (t.campusIds ?? []).includes(campus!.id!))}
          assignments={(assignments.data ?? []).filter((a) => a.courseId === course.id)}
          editable={editable}
          pending={assign.isPending}
          onChange={(subjectId, teacherId) => assign.mutate({ courseId: course.id!, subjectId, teacherId })}
        />
      ))}
    </Stack>
  );
}

/** Materias con horas por grado (por nombre del grado), tomadas del plan de estudios. */
function rowsOf(plan: StudyPlanDto): Map<string, Row[]> {
  const result = new Map<string, Row[]>();
  for (const grade of plan.grades ?? []) {
    const rows: Row[] = [];
    for (const area of plan.areas ?? []) {
      for (const subject of area.subjects ?? []) {
        const item = (subject.items ?? []).find((i) => i.gradeId === grade.id);
        if (!item || item.deliveryMode === 'Transversal' || (item.weeklyHours ?? 0) <= 0) continue;
        rows.push({ subjectId: subject.id!, subjectName: subject.name ?? '', areaId: area.id!, hours: item.weeklyHours ?? 0 });
      }
    }
    result.set(grade.name ?? '', rows);
  }
  return result;
}

function CourseAssignments({
  course,
  rows,
  teachers,
  assignments,
  editable,
  pending,
  onChange
}: {
  course: CourseDto;
  rows: Row[];
  teachers: TeacherDto[];
  assignments: TeachingAssignmentDto[];
  editable: boolean;
  pending: boolean;
  onChange: (subjectId: string, teacherId: string | null) => void;
}) {
  const without = rows.filter((r) => !assignments.find((a) => a.subjectId === r.subjectId)?.teacherId).length;

  return (
    <Paper withBorder>
      <Group justify="space-between" p="md" pb="xs">
        <Title order={4}>{course.name}</Title>
        {rows.length > 0 && (
          <Badge variant="light" color={without === 0 ? 'green' : 'yellow'}>
            {without === 0 ? 'Todas con docente' : `${without} sin docente`}
          </Badge>
        )}
      </Group>
      <Table verticalSpacing="xs">
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Materia</Table.Th>
            <Table.Th w={90}>Horas</Table.Th>
            <Table.Th w={320}>Docente</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {rows.map((row) => {
            const assignment = assignments.find((a) => a.subjectId === row.subjectId);
            const candidates = teachers.filter((t) => (t.areaIds ?? []).includes(row.areaId));
            const current = assignment?.teacherId ?? null;
            // Un docente asignado que ya no es candidato (inactivo o sin el área) se sigue mostrando
            const known = current && !candidates.some((t) => t.id === current) ? teachers.find((t) => t.id === current) : undefined;
            const options = [...candidates, ...(known ? [known] : [])].map((t) => ({ value: t.id!, label: t.fullName ?? '' }));

            return (
              <Table.Tr key={row.subjectId}>
                <Table.Td fw={500}>{row.subjectName}</Table.Td>
                <Table.Td>{row.hours} h</Table.Td>
                <Table.Td>
                  <Group gap="xs" wrap="nowrap">
                    <Select
                      aria-label={`Docente de ${row.subjectName} en ${course.name}`}
                      placeholder={candidates.length === 0 ? 'Sin docentes de esta área' : 'Sin docente'}
                      size="xs"
                      clearable
                      searchable
                      flex={1}
                      disabled={!editable || pending}
                      data={options}
                      value={current}
                      onChange={(v) => onChange(row.subjectId, v)}
                    />
                    {current && !assignment?.isManual && (
                      <Badge size="xs" variant="light" color="gray">
                        Propuesto
                      </Badge>
                    )}
                  </Group>
                </Table.Td>
              </Table.Tr>
            );
          })}
          {rows.length === 0 && (
            <Table.Tr>
              <Table.Td colSpan={3}>
                <Text size="sm" c="dimmed">
                  El plan de estudios no tiene materias con horas para este grado.
                </Text>
              </Table.Td>
            </Table.Tr>
          )}
        </Table.Tbody>
      </Table>
    </Paper>
  );
}
