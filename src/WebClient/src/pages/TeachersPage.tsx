import { ActionIcon, Badge, Button, Group, Modal, MultiSelect, NumberInput, Paper, SimpleGrid, Stack, Switch, Table, Text, TextInput, Title, Tooltip } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconCalendarOff, IconEdit, IconPlus, IconUserOff, IconUsers } from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { api, type TeacherDto } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { EmptyState, ErrorAlert, Loading } from '../components/PageState';
import { useSchool } from '../context/SchoolContext';
import { AvailabilityGrid } from '../features/teachers/AvailabilityGrid';

/** Docentes: datos, áreas que pueden dictar, sedes y carga máxima. */
export function TeachersPage() {
  const teachers = useQuery({ queryKey: ['teachers'], queryFn: () => api.catalog.teachers.get() });
  const { campuses } = useSchool();
  const { isAdmin, canManageCampus } = useAuth();
  const [target, setTarget] = useState<{ teacher?: TeacherDto } | null>(null);
  const [availabilityOf, setAvailabilityOf] = useState<TeacherDto | null>(null);
  const [search, setSearch] = useState('');

  if (teachers.isLoading) return <Loading label="Cargando docentes…" />;
  if (teachers.error) return <ErrorAlert error={teachers.error} />;

  const campusNames = (ids?: string[] | null) =>
    (ids ?? []).map((id) => campuses.find((c) => c.id === id)?.name).filter(Boolean).join(', ') || '—';

  const term = search.trim().toLowerCase();
  const list = (teachers.data ?? []).filter(
    (t) => !term || `${t.fullName} ${t.email ?? ''}`.toLowerCase().includes(term)
  );

  return (
    <Stack gap="md">
      <Group justify="space-between">
        <div>
          <Title order={2}>Docentes</Title>
          <Text c="dimmed" size="sm">
            Registre los docentes y las áreas que pueden dictar. Un horario se puede generar sin docentes y asignarlos después.
          </Text>
        </div>
        {isAdmin && (
          <Button leftSection={<IconPlus size={16} />} onClick={() => setTarget({})}>
            Nuevo docente
          </Button>
        )}
      </Group>

      {(teachers.data ?? []).length === 0 ? (
        <EmptyState icon={IconUsers} title="Aún no hay docentes registrados">
          <Text c="dimmed" size="sm">
            Puede generar el horario ahora y asignar los docentes cuando los registre.
          </Text>
        </EmptyState>
      ) : (
        <Paper withBorder>
          <Group p="md" pb={0}>
            <TextInput
              placeholder="Buscar por nombre o correo"
              aria-label="Buscar docente"
              w={320}
              value={search}
              onChange={(e) => setSearch(e.currentTarget.value)}
            />
          </Group>
          <Table verticalSpacing="sm" mt="sm">
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Docente</Table.Th>
                <Table.Th>Áreas</Table.Th>
                <Table.Th>Sedes</Table.Th>
                <Table.Th w={110}>Carga máx.</Table.Th>
                <Table.Th w={110}>Estado</Table.Th>
                <Table.Th w={100} />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {list.map((t) => (
                <Table.Tr key={t.id} c={t.isActive ? undefined : 'dimmed'}>
                  <Table.Td>
                    <Text fw={600} size="sm">
                      {t.fullName}
                    </Text>
                    <Text size="xs" c="dimmed">
                      {t.email ?? 'Sin correo'}
                    </Text>
                  </Table.Td>
                  <Table.Td>
                    <Group gap={4}>
                      {(t.areas ?? []).length === 0 && (
                        <Text size="sm" c="dimmed">
                          Sin áreas
                        </Text>
                      )}
                      {(t.areas ?? []).map((a) => (
                        <Badge key={a} variant="light" size="sm">
                          {a}
                        </Badge>
                      ))}
                    </Group>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">{campusNames(t.campusIds)}</Text>
                  </Table.Td>
                  <Table.Td>{t.maxWeeklyHours} h/sem</Table.Td>
                  <Table.Td>
                    <Badge variant="light" color={t.isActive ? 'green' : 'gray'}>
                      {t.isActive ? 'Activo' : 'Inactivo'}
                    </Badge>
                  </Table.Td>
                  <Table.Td>
                    <Group gap={4} wrap="nowrap">
                      <Tooltip label="Disponibilidad semanal">
                        <ActionIcon
                          variant="subtle"
                          aria-label={`Disponibilidad de ${t.fullName}`}
                          onClick={() => setAvailabilityOf(t)}
                        >
                          <IconCalendarOff size={16} />
                        </ActionIcon>
                      </Tooltip>
                      {isAdmin && (
                        <Tooltip label="Editar docente">
                          <ActionIcon variant="subtle" aria-label={`Editar ${t.fullName}`} onClick={() => setTarget({ teacher: t })}>
                            <IconEdit size={16} />
                          </ActionIcon>
                        </Tooltip>
                      )}
                    </Group>
                  </Table.Td>
                </Table.Tr>
              ))}
              {list.length === 0 && (
                <Table.Tr>
                  <Table.Td colSpan={6}>
                    <Group gap="xs" c="dimmed">
                      <IconUserOff size={16} />
                      <Text size="sm">Ningún docente coincide con la búsqueda.</Text>
                    </Group>
                  </Table.Td>
                </Table.Tr>
              )}
            </Table.Tbody>
          </Table>
        </Paper>
      )}

      {target && <TeacherModal teacher={target.teacher} onClose={() => setTarget(null)} />}
      {availabilityOf && (
        <Modal opened onClose={() => setAvailabilityOf(null)} title={`Disponibilidad de ${availabilityOf.fullName}`} size="xl">
          <AvailabilityGrid
            teacher={teachers.data?.find((t) => t.id === availabilityOf.id) ?? availabilityOf}
            campuses={campuses}
            editable={isAdmin || (availabilityOf.campusIds ?? []).some((id) => canManageCampus(id))}
          />
        </Modal>
      )}
    </Stack>
  );
}

function TeacherModal({ teacher, onClose }: { teacher?: TeacherDto; onClose: () => void }) {
  const queryClient = useQueryClient();
  const { campuses } = useSchool();
  const areas = useQuery({ queryKey: ['areas'], queryFn: () => api.areas.get() });

  const [firstName, setFirstName] = useState(teacher?.firstName ?? '');
  const [lastName, setLastName] = useState(teacher?.lastName ?? '');
  const [email, setEmail] = useState(teacher?.email ?? '');
  const [phone, setPhone] = useState(teacher?.phone ?? '');
  const [areaIds, setAreaIds] = useState<string[]>(teacher?.areaIds ?? []);
  const [campusIds, setCampusIds] = useState<string[]>(teacher?.campusIds ?? (campuses.length === 1 ? [campuses[0].id!] : []));
  const [maxWeekly, setMaxWeekly] = useState<number | string>(teacher?.maxWeeklyHours ?? 22);
  const [maxDaily, setMaxDaily] = useState<number | string>(teacher?.maxDailyHours ?? '');
  const [isActive, setIsActive] = useState(teacher?.isActive ?? true);

  const save = useMutation({
    mutationFn: async () => {
      const body = {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim() || null,
        phone: phone.trim() || null,
        areaIds,
        campusIds,
        maxWeeklyHours: Number(maxWeekly),
        maxDailyHours: maxDaily === '' ? null : Number(maxDaily),
        maxGapsPerDay: teacher?.maxGapsPerDay ?? null
      };
      if (teacher) await api.teachers.byTeacherId(teacher.id!).put({ ...body, isActive });
      else await api.teachers.post(body);
    },
    onSuccess: () => {
      notifications.show({ color: 'green', message: teacher ? 'Docente actualizado' : 'Docente registrado' });
      queryClient.invalidateQueries({ queryKey: ['teachers'] });
      onClose();
    }
  });

  const valid = firstName.trim() && lastName.trim() && Number(maxWeekly) >= 1;

  return (
    <Modal opened onClose={onClose} title={teacher ? `Editar a ${teacher.fullName}` : 'Nuevo docente'} size="lg">
      <Stack>
        {save.error && <ErrorAlert error={save.error} title="No se pudo guardar" />}
        <SimpleGrid cols={{ base: 1, sm: 2 }}>
          <TextInput label="Nombres" required data-autofocus value={firstName} onChange={(e) => setFirstName(e.currentTarget.value)} />
          <TextInput label="Apellidos" required value={lastName} onChange={(e) => setLastName(e.currentTarget.value)} />
          <TextInput label="Correo" type="email" value={email} onChange={(e) => setEmail(e.currentTarget.value)} />
          <TextInput label="Teléfono" value={phone} onChange={(e) => setPhone(e.currentTarget.value)} />
        </SimpleGrid>
        <MultiSelect
          label="Áreas que puede dictar"
          description="El generador solo propone al docente para materias de estas áreas"
          searchable
          data={(areas.data ?? []).map((a) => ({ value: a.id!, label: a.name ?? '' }))}
          value={areaIds}
          onChange={setAreaIds}
        />
        <MultiSelect
          label="Sedes donde trabaja"
          data={campuses.map((c) => ({ value: c.id!, label: c.name ?? '' }))}
          value={campusIds}
          onChange={setCampusIds}
        />
        <SimpleGrid cols={{ base: 1, sm: 2 }}>
          <NumberInput label="Horas de clase por semana (máx.)" required min={1} max={60} allowDecimal={false} value={maxWeekly} onChange={setMaxWeekly} />
          <NumberInput
            label="Horas de clase por día (máx.)"
            description="Opcional"
            min={1}
            allowDecimal={false}
            value={maxDaily}
            onChange={setMaxDaily}
          />
        </SimpleGrid>
        {teacher && (
          <Switch
            label="Docente activo"
            description="Un docente inactivo deja de proponerse en nuevos horarios; sus clases actuales se conservan"
            checked={isActive}
            onChange={(e) => setIsActive(e.currentTarget.checked)}
          />
        )}
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            Cancelar
          </Button>
          <Button loading={save.isPending} disabled={!valid} onClick={() => save.mutate()}>
            Guardar
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
}
