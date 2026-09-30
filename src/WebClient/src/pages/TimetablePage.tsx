import { Alert, Badge, Button, Group, Loader, Modal, Progress, SegmentedControl, Select, Stack, Tabs, Text, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconArrowLeft, IconCalendarWeek, IconSchool, IconSend, IconSparkles, IconUser } from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router';
import { api, type LessonDto } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { ErrorAlert, Loading } from '../components/PageState';
import { TimetableGrid } from '../features/timetable/TimetableGrid';
import { buildDayGrid, buildWeekGrid, DAY_NAMES, WEEKDAYS, type GridLesson, type Weekday } from '../features/timetable/weekGrid';
import { errorMessage } from '../lib/errors';
import { JOB_STATUS_LABELS, TIMETABLE_STATUS_LABELS } from '../lib/labels';

function toGridLesson(l: LessonDto): GridLesson {
  return {
    id: l.id ?? '',
    day: (l.day ?? 'Monday') as Weekday,
    periodNumber: l.periodNumber ?? 0,
    start: l.start?.toString(),
    end: l.end?.toString(),
    shiftName: l.shiftName ?? '',
    courseId: l.courseId ?? '',
    courseName: l.courseName ?? '',
    subjectName: l.subjectName ?? '',
    subjectCode: l.subjectCode,
    teacherId: l.teacherId,
    teacherName: l.teacherName,
    spaceName: l.spaceName
  };
}

const jobKey = (timetableId: string) => `horarios.job.${timetableId}`;

export function TimetablePage() {
  const { timetableId = '' } = useParams();
  const { user, canManageCampus } = useAuth();
  const queryClient = useQueryClient();

  const timetables = useQuery({ queryKey: ['timetables', 'all'], queryFn: () => api.timetables.get() });
  const timetable = timetables.data?.find((t) => t.id === timetableId);

  const lessons = useQuery({
    queryKey: ['lessons', timetableId],
    queryFn: () => api.timetables.byTimetableId(timetableId).lessons.get(),
    enabled: !!timetable
  });
  const courses = useQuery({
    queryKey: ['courses', timetable?.academicYearId, timetable?.campusId],
    queryFn: () => api.catalog.courses.get({ queryParameters: { academicYearId: timetable!.academicYearId!, campusId: timetable!.campusId! } }),
    enabled: !!timetable
  });

  const gridLessons = useMemo(() => (lessons.data ?? []).map(toGridLesson), [lessons.data]);

  // Solicitud de generación en curso (se recuerda si se recarga la página)
  const [jobId, setJobId] = useState<string | null>(() => {
    try {
      return localStorage.getItem(jobKey(timetableId));
    } catch {
      return null;
    }
  });
  const job = useQuery({
    queryKey: ['generation-job', jobId],
    queryFn: () => api.generationJobs.byJobId(jobId!).get(),
    enabled: !!jobId,
    refetchInterval: (query) => (query.state.data?.isFinished ? false : 3000)
  });

  useEffect(() => {
    if (!job.data?.isFinished) return;
    try {
      localStorage.removeItem(jobKey(timetableId));
    } catch {
      // sin almacenamiento
    }
    queryClient.invalidateQueries({ queryKey: ['lessons', timetableId] });
    queryClient.invalidateQueries({ queryKey: ['timetables'] });
  }, [job.data?.isFinished, timetableId, queryClient]);

  const publish = useMutation({
    mutationFn: () => api.timetables.byTimetableId(timetableId).publish.post(),
    onSuccess: () => {
      notifications.show({ color: 'green', message: 'Horario publicado: ya lo ven los docentes' });
      queryClient.invalidateQueries({ queryKey: ['timetables'] });
    },
    onError: (e) => notifications.show({ color: 'red', title: 'No se pudo publicar', message: errorMessage(e) })
  });

  if (timetables.isLoading) return <Loading />;
  if (timetables.error) return <ErrorAlert error={timetables.error} />;
  if (!timetable) return <ErrorAlert error={{ responseStatusCode: 404 }} title="Horario no encontrado" />;

  const isDraft = timetable.status === 'Draft';
  const canManage = canManageCampus(timetable.campusId) && isDraft;
  const running = !!jobId && !job.data?.isFinished;

  return (
    <Stack gap="md">
      <Group justify="space-between" align="flex-start">
        <Stack gap={4}>
          <Button component={Link} to="/horarios" variant="subtle" size="compact-sm" leftSection={<IconArrowLeft size={14} />} w="fit-content">
            Horarios
          </Button>
          <Group gap="sm">
            <Title order={2}>{timetable.name}</Title>
            <Badge color={isDraft ? 'yellow' : 'green'} variant="light" size="lg">
              {TIMETABLE_STATUS_LABELS[timetable.status ?? ''] ?? timetable.status}
            </Badge>
          </Group>
          <Text c="dimmed" size="sm">
            {timetable.lessonCount} clases
            {timetable.publishedAt ? ` · Publicado el ${timetable.publishedAt.toLocaleDateString('es-CO')}` : ''}
          </Text>
        </Stack>
        {canManage && (
          <Group>
            <GenerateButton timetableId={timetableId} disabled={running} onQueued={setJobId} />
            <Button
              color="green"
              leftSection={<IconSend size={16} />}
              disabled={running || (timetable.lessonCount ?? 0) === 0}
              loading={publish.isPending}
              onClick={() => publish.mutate()}
            >
              Publicar
            </Button>
          </Group>
        )}
      </Group>

      {jobId && job.data && (
        <Alert
          color={running ? 'blue' : job.data.status === 'Succeeded' ? 'green' : job.data.status === 'PartiallySucceeded' ? 'yellow' : 'red'}
          icon={running ? <Loader size={18} /> : <IconSparkles size={18} />}
          title={JOB_STATUS_LABELS[job.data.status ?? ''] ?? job.data.status}
          withCloseButton={!running}
          onClose={() => setJobId(null)}
          data-testid="generation-status"
        >
          {running ? (
            <Stack gap={6}>
              <Text size="sm">Buscando la mejor distribución (hasta {Math.round((job.data.timeLimitSeconds ?? 0) / 60)} min)…</Text>
              <Progress value={100} animated striped />
            </Stack>
          ) : (
            <Text size="sm" style={{ whiteSpace: 'pre-line' }}>
              {job.data.placedLessons} clases ubicadas
              {job.data.unplacedLessons ? `, ${job.data.unplacedLessons} sin ubicar` : ''}.{'\n'}
              {job.data.message}
            </Text>
          )}
        </Alert>
      )}

      {lessons.isLoading || courses.isLoading ? (
        <Loading label="Cargando clases…" />
      ) : lessons.error ? (
        <ErrorAlert error={lessons.error} />
      ) : (
        <Views
          lessons={gridLessons}
          courses={(courses.data ?? []).map((c) => ({ key: c.id!, label: c.name ?? '' }))}
          ownTeacherId={user?.teacherId ?? undefined}
          teachersFromLessons={lessons.data ?? []}
        />
      )}
    </Stack>
  );
}

function GenerateButton({ timetableId, disabled, onQueued }: { timetableId: string; disabled: boolean; onQueued: (jobId: string) => void }) {
  const [opened, setOpened] = useState(false);
  const [minutes, setMinutes] = useState('3');

  const generate = useMutation({
    mutationFn: () => api.timetables.byTimetableId(timetableId).generate.post({ queryParameters: { timeLimitSeconds: Number(minutes) * 60 } }),
    onSuccess: (queued) => {
      if (!queued?.jobId) return;
      try {
        localStorage.setItem(jobKey(timetableId), queued.jobId);
      } catch {
        // sin almacenamiento
      }
      onQueued(queued.jobId);
      setOpened(false);
    }
  });

  return (
    <>
      <Button leftSection={<IconSparkles size={16} />} disabled={disabled} onClick={() => setOpened(true)}>
        Generar
      </Button>
      <Modal opened={opened} onClose={() => setOpened(false)} title="Generar el horario">
        <Stack>
          {generate.error && <ErrorAlert error={generate.error} title="No se pudo iniciar" />}
          <Text size="sm">
            Se asignan docentes y espacios y se ubican las clases según el plan de estudios. Las clases fijadas se respetan; las
            demás se reemplazan.
          </Text>
          <div>
            <Text size="sm" fw={500} mb={4}>
              Tiempo máximo de búsqueda
            </Text>
            <SegmentedControl
              fullWidth
              value={minutes}
              onChange={setMinutes}
              data={[
                { value: '1', label: '1 min' },
                { value: '3', label: '3 min' },
                { value: '5', label: '5 min' }
              ]}
            />
            <Text size="xs" c="dimmed" mt={4}>
              Termina antes si encuentra la mejor solución. Con más tiempo, menos huecos.
            </Text>
          </div>
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setOpened(false)}>
              Cancelar
            </Button>
            <Button loading={generate.isPending} onClick={() => generate.mutate()}>
              Generar
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}

function Views({
  lessons,
  courses,
  ownTeacherId,
  teachersFromLessons
}: {
  lessons: GridLesson[];
  courses: { key: string; label: string }[];
  ownTeacherId?: string;
  teachersFromLessons: LessonDto[];
}) {
  const teachers = useMemo(() => {
    const map = new Map<string, string>();
    teachersFromLessons.forEach((l) => l.teacherId && map.set(l.teacherId, l.teacherName ?? ''));
    return [...map.entries()].map(([value, label]) => ({ value, label })).sort((a, b) => a.label.localeCompare(b.label));
  }, [teachersFromLessons]);

  const [tab, setTab] = useState<string | null>(ownTeacherId ? 'teacher' : 'course');
  const [courseId, setCourseId] = useState<string | null>(courses[0]?.key ?? null);
  const [teacherId, setTeacherId] = useState<string | null>(ownTeacherId ?? null);
  const [day, setDay] = useState<Weekday>('Monday');

  const selectedTeacher = teacherId ?? teachers[0]?.value ?? null;
  const courseGrid = useMemo(() => buildWeekGrid(lessons.filter((l) => l.courseId === courseId)), [lessons, courseId]);
  const teacherGrid = useMemo(
    () => buildWeekGrid(lessons.filter((l) => l.teacherId === selectedTeacher)),
    [lessons, selectedTeacher]
  );
  const dayGrid = useMemo(() => buildDayGrid(lessons.filter((l) => l.day === day), courses), [lessons, day, courses]);

  const days = WEEKDAYS.filter((d) => (d !== 'Saturday' && d !== 'Sunday') || lessons.some((l) => l.day === d));

  return (
    <Tabs value={tab} onChange={setTab} keepMounted={false}>
      <Tabs.List mb="md">
        <Tabs.Tab value="course" leftSection={<IconSchool size={16} />}>
          Por curso
        </Tabs.Tab>
        <Tabs.Tab value="teacher" leftSection={<IconUser size={16} />}>
          Por docente
        </Tabs.Tab>
        <Tabs.Tab value="day" leftSection={<IconCalendarWeek size={16} />}>
          Institucional
        </Tabs.Tab>
      </Tabs.List>

      <Tabs.Panel value="course">
        <Stack>
          <Select label="Curso" data={courses.map((c) => ({ value: c.key, label: c.label }))} value={courseId} onChange={setCourseId} w={220} searchable />
          <TimetableGrid grid={courseGrid} mode="course" />
        </Stack>
      </Tabs.Panel>

      <Tabs.Panel value="teacher">
        <Stack>
          <Select label="Docente" data={teachers} value={selectedTeacher} onChange={setTeacherId} w={300} searchable />
          <TimetableGrid grid={teacherGrid} mode="teacher" />
        </Stack>
      </Tabs.Panel>

      <Tabs.Panel value="day">
        <Stack>
          <SegmentedControl value={day} onChange={(v) => setDay(v as Weekday)} data={days.map((d) => ({ value: d, label: DAY_NAMES[d] }))} w="fit-content" />
          <TimetableGrid grid={dayGrid} mode="day" />
        </Stack>
      </Tabs.Panel>
    </Tabs>
  );
}
