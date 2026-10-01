import { Badge, Button, Group, Modal, Paper, Select, Stack, Table, Text, TextInput, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconChevronRight, IconPlus, IconTable } from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useNavigate } from 'react-router';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { EmptyState, ErrorAlert, Loading } from '../components/PageState';
import { useSchool } from '../context/SchoolContext';
import { TIMETABLE_STATUS_LABELS } from '../lib/labels';

const STATUS_COLORS: Record<string, string> = { Draft: 'yellow', Published: 'green', Archived: 'gray' };

export function TimetablesPage() {
  const { year, campus, loading } = useSchool();
  const { canManageCampus } = useAuth();
  const navigate = useNavigate();

  const timetables = useQuery({
    queryKey: ['timetables', year?.id, campus?.id],
    queryFn: () => api.timetables.get({ queryParameters: { academicYearId: year!.id!, campusId: campus!.id! } }),
    enabled: !!year?.id && !!campus?.id
  });

  if (loading || timetables.isLoading) return <Loading label="Cargando horarios…" />;
  if (timetables.error) return <ErrorAlert error={timetables.error} />;

  const periodName = (id?: string | null) => year?.periods?.find((p) => p.id === id)?.name ?? '';
  const list = timetables.data ?? [];
  const canManage = canManageCampus(campus?.id);

  return (
    <Stack gap="md">
      <Group justify="space-between">
        <Stack gap={2}>
          <Title order={2}>Horarios</Title>
          <Text c="dimmed" size="sm">
            {campus?.name} · Año lectivo {year?.year}
          </Text>
        </Stack>
        {canManage && list.length > 0 && <CreateTimetableButton />}
      </Group>

      {list.length === 0 ? (
        <EmptyState icon={IconTable} title="Aún no hay horarios">
          {canManage ? (
            <>
              <Text c="dimmed" size="sm" ta="center" maw={420}>
                Cree un horario para un periodo y genérelo a partir del plan de estudios de la sede.
              </Text>
              <CreateTimetableButton />
            </>
          ) : (
            <Text c="dimmed" size="sm">
              Los horarios aparecen aquí cuando se publican.
            </Text>
          )}
        </EmptyState>
      ) : (
        <Paper withBorder>
          <Table highlightOnHover verticalSpacing="sm">
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Nombre</Table.Th>
                <Table.Th>Periodo</Table.Th>
                <Table.Th>Estado</Table.Th>
                <Table.Th ta="right">Clases</Table.Th>
                <Table.Th w={40} />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {list.map((t) => (
                <Table.Tr key={t.id} style={{ cursor: 'pointer' }} onClick={() => navigate(`/horarios/${t.id}`)}>
                  <Table.Td fw={600}>{t.name}</Table.Td>
                  <Table.Td>{periodName(t.academicPeriodId)}</Table.Td>
                  <Table.Td>
                    <Badge color={STATUS_COLORS[t.status ?? ''] ?? 'gray'} variant="light">
                      {TIMETABLE_STATUS_LABELS[t.status ?? ''] ?? t.status}
                    </Badge>
                  </Table.Td>
                  <Table.Td ta="right">{t.lessonCount}</Table.Td>
                  <Table.Td>
                    <IconChevronRight size={16} />
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Paper>
      )}
    </Stack>
  );
}

function CreateTimetableButton() {
  const { year, campus } = useSchool();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [opened, setOpened] = useState(false);
  const [periodId, setPeriodId] = useState<string | null>(null);
  const [name, setName] = useState('');

  const create = useMutation({
    mutationFn: () =>
      api.timetables.post({ academicYearId: year!.id!, campusId: campus!.id!, academicPeriodId: periodId!, name: name.trim() || null }),
    onSuccess: (created) => {
      notifications.show({ color: 'green', message: 'Horario creado; ahora puede generarlo' });
      queryClient.invalidateQueries({ queryKey: ['timetables'] });
      setOpened(false);
      if (created?.id) navigate(`/horarios/${created.id}`);
    }
  });

  return (
    <>
      <Button leftSection={<IconPlus size={16} />} onClick={() => setOpened(true)}>
        Nuevo horario
      </Button>
      <Modal opened={opened} onClose={() => setOpened(false)} title="Nuevo horario">
        <Stack>
          {create.error && <ErrorAlert error={create.error} title="No se pudo crear" />}
          <Select
            label="Periodo académico"
            required
            data={(year?.periods ?? []).map((p) => ({ value: p.id!, label: p.name ?? '' }))}
            value={periodId}
            onChange={setPeriodId}
          />
          <TextInput label="Nombre" placeholder="Se genera automáticamente" value={name} onChange={(e) => setName(e.currentTarget.value)} />
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setOpened(false)}>
              Cancelar
            </Button>
            <Button loading={create.isPending} disabled={!periodId} onClick={() => create.mutate()}>
              Crear
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}
