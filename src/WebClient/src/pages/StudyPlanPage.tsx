import { Alert, Badge, Button, Group, Modal, SegmentedControl, Select, Stack, Text, Textarea, TextInput, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconBook2, IconCircleCheck, IconEdit, IconInfoCircle, IconLockOpen, IconPlus } from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { api, type StudyPlanDto } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { EmptyState, ErrorAlert, Loading } from '../components/PageState';
import { useSchool } from '../context/SchoolContext';
import { ItemEditorModal } from '../features/studyPlan/ItemEditorModal';
import { StudyPlanMatrix, type CellTarget } from '../features/studyPlan/StudyPlanMatrix';
import { errorMessage } from '../lib/errors';
import { PLAN_STATUS_LABELS } from '../lib/labels';

export function StudyPlanPage() {
  const { year, campus, loading } = useSchool();
  const { canManageCampus } = useAuth();

  const plans = useQuery({
    queryKey: ['study-plans', year?.id, campus?.id],
    queryFn: () => api.studyPlans.get({ queryParameters: { academicYearId: year!.id!, campusId: campus!.id! } }),
    enabled: !!year?.id && !!campus?.id
  });

  const planId = plans.data?.[0]?.id ?? undefined;
  const plan = useQuery({
    queryKey: ['study-plan', planId],
    queryFn: () => api.studyPlans.byStudyPlanId(planId!).get(),
    enabled: !!planId
  });

  if (loading || plans.isLoading || plan.isLoading) return <Loading label="Cargando el plan de estudios…" />;
  if (plans.error) return <ErrorAlert error={plans.error} />;
  if (plan.error) return <ErrorAlert error={plan.error} />;

  if (!planId || !plan.data)
    return (
      <EmptyState icon={IconBook2} title={`No hay plan de estudios ${year?.year ?? ''} para ${campus?.name ?? 'esta sede'}`}>
        {canManageCampus(campus?.id) ? (
          <CreatePlanButton />
        ) : (
          <Text c="dimmed" size="sm">
            El coordinador de la sede o el administrador lo crea.
          </Text>
        )}
      </EmptyState>
    );

  return <PlanView plan={plan.data} editable={canManageCampus(plan.data.campusId) && plan.data.status === 'Draft'} canManage={canManageCampus(plan.data.campusId)} />;
}

function PlanView({ plan, editable, canManage }: { plan: StudyPlanDto; editable: boolean; canManage: boolean }) {
  const queryClient = useQueryClient();
  const [periodId, setPeriodId] = useState<string>('general');
  const [target, setTarget] = useState<CellTarget | null>(null);
  const [editing, setEditing] = useState(false);

  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ['study-plan', plan.id] });
    queryClient.invalidateQueries({ queryKey: ['study-plans'] });
  };

  const approve = useMutation({
    mutationFn: () => api.studyPlans.byStudyPlanId(plan.id!).approve.post(),
    onSuccess: () => {
      notifications.show({ color: 'green', message: 'Plan aprobado' });
      refresh();
    },
    onError: (e) => notifications.show({ color: 'red', title: 'No se pudo aprobar', message: errorMessage(e) })
  });

  const reopen = useMutation({
    mutationFn: () => api.studyPlans.byStudyPlanId(plan.id!).reopen.post(),
    onSuccess: () => {
      notifications.show({ color: 'blue', message: 'Plan reabierto para cambios' });
      refresh();
    },
    onError: (e) => notifications.show({ color: 'red', title: 'No se pudo reabrir', message: errorMessage(e) })
  });

  const approved = plan.status === 'Approved';

  return (
    <Stack gap="md">
      <Group justify="space-between" align="flex-start">
        <Stack gap={4}>
          <Group gap="sm">
            <Title order={2}>{plan.name}</Title>
            <Badge color={approved ? 'green' : 'yellow'} variant="light" size="lg">
              {PLAN_STATUS_LABELS[plan.status ?? 'Draft']}
            </Badge>
          </Group>
          <Text c="dimmed" size="sm">
            {plan.campusName} · Año lectivo {plan.year}
            {approved && plan.approvedAt ? ` · Aprobado el ${plan.approvedAt.toLocaleDateString('es-CO')}` : ''}
          </Text>
        </Stack>
        {canManage && (
          <Group>
            {!approved && (
              <Button variant="default" leftSection={<IconEdit size={16} />} onClick={() => setEditing(true)}>
                Nombre y notas
              </Button>
            )}
            {approved ? (
              <Button variant="default" leftSection={<IconLockOpen size={16} />} loading={reopen.isPending} onClick={() => reopen.mutate()}>
                Reabrir
              </Button>
            ) : (
              <Button color="green" leftSection={<IconCircleCheck size={16} />} loading={approve.isPending} onClick={() => approve.mutate()}>
                Aprobar
              </Button>
            )}
          </Group>
        )}
      </Group>

      <Group justify="space-between">
        <SegmentedControl
          value={periodId}
          onChange={setPeriodId}
          data={[{ value: 'general', label: 'IH general' }, ...(plan.periods ?? []).map((p) => ({ value: p.id!, label: p.name ?? '' }))]}
        />
        <Text size="xs" c="dimmed">
          {editable
            ? 'Haga clic en una celda para agregar o cambiar la asignatura en ese grado.'
            : approved
              ? 'Plan aprobado: reábralo para hacer cambios.'
              : 'Solo consulta.'}
          {periodId !== 'general' && ' En naranja, las IH ajustadas para el periodo.'}
        </Text>
      </Group>

      <StudyPlanMatrix plan={plan} periodId={periodId === 'general' ? undefined : periodId} editable={editable} onCellClick={setTarget} />

      {plan.notes && (
        <Alert icon={<IconInfoCircle size={18} />} color="gray" title="Notas">
          <Text size="sm" style={{ whiteSpace: 'pre-line' }}>
            {plan.notes}
          </Text>
        </Alert>
      )}

      {target && <ItemEditorModal plan={plan} target={target} onClose={() => setTarget(null)} />}
      {editing && <EditPlanModal plan={plan} onClose={() => setEditing(false)} />}
    </Stack>
  );
}

function EditPlanModal({ plan, onClose }: { plan: StudyPlanDto; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [name, setName] = useState(plan.name ?? '');
  const [notes, setNotes] = useState(plan.notes ?? '');

  const save = useMutation({
    mutationFn: () => api.studyPlans.byStudyPlanId(plan.id!).put({ name, notes: notes.trim() || null }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['study-plan', plan.id] });
      queryClient.invalidateQueries({ queryKey: ['study-plans'] });
      onClose();
    }
  });

  return (
    <Modal opened onClose={onClose} title="Nombre y notas del plan">
      <Stack>
        {save.error && <ErrorAlert error={save.error} title="No se pudo guardar" />}
        <TextInput label="Nombre" required value={name} onChange={(e) => setName(e.currentTarget.value)} maxLength={150} />
        <Textarea
          label="Notas generales"
          description="Se imprimen al pie del documento del plan"
          autosize
          minRows={3}
          value={notes}
          onChange={(e) => setNotes(e.currentTarget.value)}
        />
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            Cancelar
          </Button>
          <Button loading={save.isPending} onClick={() => save.mutate()} disabled={!name.trim()}>
            Guardar
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
}

function CreatePlanButton() {
  const { year, campus } = useSchool();
  const queryClient = useQueryClient();
  const [opened, setOpened] = useState(false);
  const [name, setName] = useState('');
  const [copyFrom, setCopyFrom] = useState<string | null>(null);

  const existing = useQuery({ queryKey: ['study-plans', 'all'], queryFn: () => api.studyPlans.get(), enabled: opened });

  const create = useMutation({
    mutationFn: () =>
      api.studyPlans.post({
        academicYearId: year!.id!,
        campusId: campus!.id!,
        name: name.trim() || null,
        copyFromStudyPlanId: copyFrom
      }),
    onSuccess: () => {
      notifications.show({ color: 'green', message: copyFrom ? 'Plan creado a partir del plan elegido' : 'Plan creado' });
      queryClient.invalidateQueries({ queryKey: ['study-plans'] });
      setOpened(false);
    }
  });

  return (
    <>
      <Button leftSection={<IconPlus size={16} />} onClick={() => setOpened(true)}>
        Crear plan de estudios
      </Button>
      <Modal opened={opened} onClose={() => setOpened(false)} title={`Plan de estudios ${year?.year ?? ''} · ${campus?.name ?? ''}`}>
        <Stack>
          {create.error && <ErrorAlert error={create.error} title="No se pudo crear" />}
          <TextInput
            label="Nombre"
            placeholder={`Plan de estudios ${year?.year ?? ''} - ${campus?.name ?? ''}`}
            value={name}
            onChange={(e) => setName(e.currentTarget.value)}
          />
          <Select
            label="Copiar de"
            description="Opcional: parte de otro plan (por ejemplo, el del año anterior) con sus asignaturas, IH y reglas"
            placeholder="Empezar vacío"
            clearable
            data={(existing.data ?? []).map((p) => ({ value: p.id!, label: `${p.year} · ${p.campusName} · ${p.name}` }))}
            value={copyFrom}
            onChange={setCopyFrom}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setOpened(false)}>
              Cancelar
            </Button>
            <Button loading={create.isPending} onClick={() => create.mutate()}>
              Crear
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}
