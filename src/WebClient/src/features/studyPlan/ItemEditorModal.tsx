import { Alert, Button, Divider, Group, Modal, NumberInput, SegmentedControl, Select, SimpleGrid, Stack, Text, TextInput } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconAlertCircle, IconTrash } from '@tabler/icons-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { api, type DeliveryMode, type SpaceType, type StudyPlanDto } from '../../api/client';
import { errorMessage } from '../../lib/errors';
import { DELIVERY_HINTS, DELIVERY_LABELS, SPACE_TYPE_LABELS } from '../../lib/labels';
import type { CellTarget } from './StudyPlanMatrix';

interface Props {
  plan: StudyPlanDto;
  target: CellTarget;
  onClose: () => void;
}

type OptionalNumber = number | '';

const toNumber = (v: OptionalNumber): number | null => (v === '' ? null : v);

/**
 * Agrega o cambia una asignatura de un grado: IH, forma de dictarla, distribución en la semana e IH por periodo.
 */
export function ItemEditorModal({ plan, target, onClose }: Props) {
  const { item, subject, grade } = target;
  const queryClient = useQueryClient();
  const planId = plan.id!;

  const [mode, setMode] = useState<DeliveryMode>(item?.deliveryMode ?? 'Regular');
  const [hours, setHours] = useState<OptionalNumber>(item?.weeklyHours ?? 2);
  const [shiftId, setShiftId] = useState<string | null>(item?.targetShiftId ?? null);
  const [integratedInto, setIntegratedInto] = useState<string | null>(item?.integratedIntoSubjectId ?? null);
  const [note, setNote] = useState(item?.note ?? '');
  const [maxPerDay, setMaxPerDay] = useState<OptionalNumber>(item?.maxHoursPerDay ?? '');
  const [maxConsecutive, setMaxConsecutive] = useState<OptionalNumber>(item?.maxConsecutiveHours ?? '');
  const [space, setSpace] = useState<string | null>(item?.requiredSpaceType ?? null);
  const [periodHours, setPeriodHours] = useState<Record<string, OptionalNumber>>(() =>
    Object.fromEntries((plan.periods ?? []).map((p) => [p.id!, item?.periodHours?.find((h) => h.academicPeriodId === p.id)?.weeklyHours ?? '']))
  );

  const otherSubjects = (plan.areas ?? [])
    .flatMap((a) => a.subjects ?? [])
    .filter((s) => s.id !== subject.id)
    .map((s) => ({ value: s.id!, label: s.name ?? '' }));

  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ['study-plan', planId] });
    queryClient.invalidateQueries({ queryKey: ['study-plans'] });
  };

  const save = useMutation({
    mutationFn: async () => {
      const plans = api.studyPlans.byStudyPlanId(planId);
      const body = {
        deliveryMode: mode,
        weeklyHours: mode === 'Transversal' ? 0 : toNumber(hours) ?? 0,
        targetShiftId: mode === 'CounterShift' ? shiftId : null,
        integratedIntoSubjectId: mode === 'Transversal' ? integratedInto : null,
        note: note.trim() || null
      };

      let itemId = item?.id;
      if (itemId) {
        await plans.items.byItemId(itemId).put(body);
      } else {
        const created = await plans.items.post({ ...body, gradeId: grade.id, subjectId: subject.id });
        itemId = created?.id ?? undefined;
      }
      if (!itemId) return;

      const distributionChanged =
        toNumber(maxPerDay) !== (item?.maxHoursPerDay ?? null) ||
        toNumber(maxConsecutive) !== (item?.maxConsecutiveHours ?? null) ||
        space !== (item?.requiredSpaceType ?? null);
      if (distributionChanged)
        await plans.items.byItemId(itemId).distribution.put({
          maxHoursPerDay: toNumber(maxPerDay),
          maxConsecutiveHours: toNumber(maxConsecutive),
          requiredSpaceType: (space as SpaceType | null) ?? null
        });

      if (mode !== 'Transversal') {
        for (const period of plan.periods ?? []) {
          const before = item?.periodHours?.find((h) => h.academicPeriodId === period.id)?.weeklyHours ?? null;
          const after = toNumber(periodHours[period.id!] ?? '');
          if (before !== after)
            await plans.items.byItemId(itemId).periods.byAcademicPeriodId(period.id!).put({ weeklyHours: after });
        }
      }
    },
    onSuccess: () => {
      notifications.show({ color: 'green', message: `${subject.name} en ${grade.shortName} guardada` });
      refresh();
      onClose();
    },
    onError: refresh
  });

  const remove = useMutation({
    mutationFn: () => api.studyPlans.byStudyPlanId(planId).items.byItemId(item!.id!).delete(),
    onSuccess: () => {
      notifications.show({ color: 'green', message: `${subject.name} quitada de ${grade.shortName}` });
      refresh();
      onClose();
    }
  });

  const error = save.error ?? remove.error;

  return (
    <Modal opened onClose={onClose} title={`${subject.name} · ${grade.name}`} size="lg">
      <Stack>
        {error && (
          <Alert color="red" icon={<IconAlertCircle size={18} />} role="alert">
            {errorMessage(error)}
          </Alert>
        )}

        <div>
          <Text size="sm" fw={500} mb={4}>
            Forma de dictarla
          </Text>
          <SegmentedControl
            fullWidth
            value={mode}
            onChange={(v) => setMode(v as DeliveryMode)}
            data={(['Regular', 'Transversal', 'CounterShift'] as DeliveryMode[]).map((m) => ({ value: m, label: DELIVERY_LABELS[m] }))}
          />
          <Text size="xs" c="dimmed" mt={4}>
            {DELIVERY_HINTS[mode]}
          </Text>
        </div>

        <SimpleGrid cols={{ base: 1, sm: 2 }}>
          {mode !== 'Transversal' && (
            <NumberInput
              label="Intensidad horaria semanal"
              min={0}
              max={40}
              value={hours}
              onChange={(v) => setHours(typeof v === 'number' ? v : '')}
              required
            />
          )}
          {mode === 'CounterShift' && (
            <Select
              label="Jornada donde se dicta"
              data={(plan.shifts ?? []).map((s) => ({ value: s.id!, label: s.name ?? '' }))}
              value={shiftId}
              onChange={setShiftId}
              required
            />
          )}
          {mode === 'Transversal' && (
            <Select
              label="Se integra en"
              placeholder="Asignatura"
              searchable
              clearable
              data={otherSubjects}
              value={integratedInto}
              onChange={setIntegratedInto}
            />
          )}
        </SimpleGrid>

        <TextInput
          label="Nota para el documento"
          description="Aparece junto al asterisco en el plan impreso"
          value={note}
          onChange={(e) => setNote(e.currentTarget.value)}
          maxLength={500}
        />

        {mode !== 'Transversal' && (
          <>
            <Divider label="Distribución en la semana" labelPosition="left" />
            <SimpleGrid cols={{ base: 1, sm: 3 }}>
              <NumberInput
                label="Máx. horas por día"
                placeholder="Sin límite"
                min={1}
                max={10}
                value={maxPerDay}
                onChange={(v) => setMaxPerDay(typeof v === 'number' ? v : '')}
              />
              <NumberInput
                label="Máx. horas seguidas"
                description="2 = bloques dobles"
                placeholder="Sin límite"
                min={1}
                max={10}
                value={maxConsecutive}
                onChange={(v) => setMaxConsecutive(typeof v === 'number' ? v : '')}
              />
              <Select
                label="Espacio requerido"
                placeholder="Salón del curso"
                clearable
                data={Object.entries(SPACE_TYPE_LABELS).map(([value, label]) => ({ value, label: label ?? value }))}
                value={space}
                onChange={setSpace}
              />
            </SimpleGrid>

            {(plan.periods ?? []).length > 0 && (
              <>
                <Divider label="IH distinta por periodo (vacío = la general)" labelPosition="left" />
                <SimpleGrid cols={{ base: 2, sm: 4 }}>
                  {(plan.periods ?? []).map((period) => (
                    <NumberInput
                      key={period.id}
                      label={period.name}
                      placeholder={String(toNumber(hours) ?? '')}
                      min={0}
                      max={40}
                      value={periodHours[period.id!] ?? ''}
                      onChange={(v) => setPeriodHours((current) => ({ ...current, [period.id!]: typeof v === 'number' ? v : '' }))}
                    />
                  ))}
                </SimpleGrid>
              </>
            )}
          </>
        )}

        <Group justify="space-between" mt="sm">
          {item ? (
            <Button variant="subtle" color="red" leftSection={<IconTrash size={16} />} loading={remove.isPending} onClick={() => remove.mutate()}>
              Quitar del grado
            </Button>
          ) : (
            <span />
          )}
          <Group>
            <Button variant="default" onClick={onClose}>
              Cancelar
            </Button>
            <Button loading={save.isPending} onClick={() => save.mutate()} disabled={mode === 'CounterShift' && !shiftId}>
              {item ? 'Guardar' : 'Agregar'}
            </Button>
          </Group>
        </Group>
      </Stack>
    </Modal>
  );
}
