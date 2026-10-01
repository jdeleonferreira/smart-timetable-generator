import { ActionIcon, Badge, Button, Group, Menu, Modal, Paper, PasswordInput, Select, Stack, Table, Text, TextInput, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconDots, IconKey, IconPlus, IconUserCheck, IconUserOff } from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { api, type UserDto } from '../api/client';
import { ROLES, useAuth } from '../auth/AuthContext';
import { ErrorAlert, Loading } from '../components/PageState';
import { useSchool } from '../context/SchoolContext';
import { errorMessage } from '../lib/errors';
import { ROLE_LABELS } from '../lib/labels';

const ROLE_COLORS: Record<string, string> = { Admin: 'red', Coordinador: 'navy', Docente: 'teal' };

export function UsersPage() {
  const { user: me } = useAuth();
  const { campuses } = useSchool();
  const queryClient = useQueryClient();
  const users = useQuery({ queryKey: ['users'], queryFn: () => api.users.get() });
  const teachers = useQuery({ queryKey: ['teachers'], queryFn: () => api.catalog.teachers.get() });
  const [creating, setCreating] = useState(false);
  const [resetting, setResetting] = useState<UserDto | null>(null);

  const setActive = useMutation({
    mutationFn: (u: UserDto) =>
      api.users.byUserId(u.id!).put({
        email: u.email,
        fullName: u.fullName,
        role: u.role,
        campusId: u.campusId,
        teacherId: u.teacherId,
        isActive: !u.isActive
      }),
    onSuccess: (_, u) => {
      notifications.show({ color: 'green', message: u.isActive ? `${u.fullName} desactivado` : `${u.fullName} activado` });
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (e) => notifications.show({ color: 'red', title: 'No se pudo cambiar', message: errorMessage(e) })
  });

  if (users.isLoading) return <Loading label="Cargando usuarios…" />;
  if (users.error) return <ErrorAlert error={users.error} />;

  const campusName = (id?: string | null) => campuses.find((c) => c.id === id)?.name;
  const teacherName = (id?: string | null) => teachers.data?.find((t) => t.id === id)?.fullName;

  return (
    <Stack gap="md">
      <Group justify="space-between">
        <Title order={2}>Usuarios</Title>
        <Button leftSection={<IconPlus size={16} />} onClick={() => setCreating(true)}>
          Nuevo usuario
        </Button>
      </Group>

      <Paper withBorder>
        <Table verticalSpacing="sm" highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Nombre</Table.Th>
              <Table.Th>Correo</Table.Th>
              <Table.Th>Rol</Table.Th>
              <Table.Th>Sede / docente</Table.Th>
              <Table.Th>Estado</Table.Th>
              <Table.Th w={50} />
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {(users.data ?? []).map((u) => (
              <Table.Tr key={u.id} c={u.isActive ? undefined : 'dimmed'}>
                <Table.Td fw={600}>{u.fullName}</Table.Td>
                <Table.Td>{u.email}</Table.Td>
                <Table.Td>
                  <Badge color={ROLE_COLORS[u.role ?? ''] ?? 'gray'} variant="light">
                    {ROLE_LABELS[u.role ?? ''] ?? u.role}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{[campusName(u.campusId), teacherName(u.teacherId)].filter(Boolean).join(' · ') || '—'}</Text>
                </Table.Td>
                <Table.Td>
                  <Badge color={u.isActive ? 'green' : 'gray'} variant="dot">
                    {u.isActive ? 'Activo' : 'Inactivo'}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  <Menu position="bottom-end">
                    <Menu.Target>
                      <ActionIcon variant="subtle" aria-label={`Acciones de ${u.fullName}`}>
                        <IconDots size={16} />
                      </ActionIcon>
                    </Menu.Target>
                    <Menu.Dropdown>
                      <Menu.Item leftSection={<IconKey size={16} />} onClick={() => setResetting(u)}>
                        Nueva contraseña
                      </Menu.Item>
                      <Menu.Item
                        leftSection={u.isActive ? <IconUserOff size={16} /> : <IconUserCheck size={16} />}
                        color={u.isActive ? 'red' : undefined}
                        disabled={u.id === me?.id}
                        onClick={() => setActive.mutate(u)}
                      >
                        {u.isActive ? 'Desactivar' : 'Activar'}
                      </Menu.Item>
                    </Menu.Dropdown>
                  </Menu>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </Paper>

      {creating && <CreateUserModal onClose={() => setCreating(false)} teachers={teachers.data ?? []} />}
      {resetting && <ResetPasswordModal user={resetting} onClose={() => setResetting(null)} />}
    </Stack>
  );
}

function CreateUserModal({ onClose, teachers }: { onClose: () => void; teachers: { id?: string | null; fullName?: string | null }[] }) {
  const { campuses } = useSchool();
  const queryClient = useQueryClient();
  const [form, setForm] = useState({ email: '', fullName: '', role: ROLES.teacher as string, password: '', campusId: null as string | null, teacherId: null as string | null });
  const set = (patch: Partial<typeof form>) => setForm((f) => ({ ...f, ...patch }));

  const create = useMutation({
    mutationFn: () =>
      api.users.post({
        email: form.email.trim(),
        fullName: form.fullName.trim(),
        role: form.role,
        password: form.password,
        campusId: form.role === ROLES.coordinator ? form.campusId : null,
        teacherId: form.role === ROLES.admin ? null : form.teacherId
      }),
    onSuccess: () => {
      notifications.show({ color: 'green', message: `Usuario ${form.email} creado` });
      queryClient.invalidateQueries({ queryKey: ['users'] });
      onClose();
    }
  });

  return (
    <Modal opened onClose={onClose} title="Nuevo usuario">
      <Stack>
        {create.error && <ErrorAlert error={create.error} title="No se pudo crear" />}
        <Select
          label="Rol"
          data={Object.values(ROLES).map((r) => ({ value: r, label: ROLE_LABELS[r] }))}
          value={form.role}
          onChange={(v) => v && set({ role: v })}
          allowDeselect={false}
        />
        <TextInput label="Nombre completo" required value={form.fullName} onChange={(e) => set({ fullName: e.currentTarget.value })} />
        <TextInput label="Correo" type="email" required value={form.email} onChange={(e) => set({ email: e.currentTarget.value })} />
        {form.role === ROLES.coordinator && (
          <Select
            label="Sede que coordina"
            required
            data={campuses.map((c) => ({ value: c.id!, label: c.name ?? '' }))}
            value={form.campusId}
            onChange={(v) => set({ campusId: v })}
          />
        )}
        {form.role !== ROLES.admin && (
          <Select
            label="Docente"
            description={form.role === ROLES.teacher ? 'Registro de docente al que corresponde la cuenta' : 'Opcional, si también dicta clase'}
            required={form.role === ROLES.teacher}
            searchable
            clearable
            data={teachers.map((t) => ({ value: t.id!, label: t.fullName ?? '' }))}
            value={form.teacherId}
            onChange={(v) => set({ teacherId: v })}
          />
        )}
        <PasswordInput
          label="Contraseña inicial"
          description="Mínimo 8 caracteres, con mayúscula, minúscula y número"
          required
          value={form.password}
          onChange={(e) => set({ password: e.currentTarget.value })}
        />
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            Cancelar
          </Button>
          <Button loading={create.isPending} onClick={() => create.mutate()}>
            Crear
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
}

function ResetPasswordModal({ user, onClose }: { user: UserDto; onClose: () => void }) {
  const [password, setPassword] = useState('');
  const reset = useMutation({
    mutationFn: () => api.users.byUserId(user.id!).resetPassword.post({ newPassword: password }),
    onSuccess: () => {
      notifications.show({ color: 'green', message: `Contraseña de ${user.fullName} cambiada; sus sesiones abiertas se cerraron` });
      onClose();
    }
  });

  return (
    <Modal opened onClose={onClose} title={`Nueva contraseña para ${user.fullName}`}>
      <Stack>
        {reset.error && <ErrorAlert error={reset.error} title="No se pudo cambiar" />}
        <PasswordInput label="Contraseña nueva" required value={password} onChange={(e) => setPassword(e.currentTarget.value)} />
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            Cancelar
          </Button>
          <Button loading={reset.isPending} disabled={password.length < 8} onClick={() => reset.mutate()}>
            Cambiar
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
}
