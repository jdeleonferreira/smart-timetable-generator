import { ActionIcon, Badge, Button, Group, Modal, NumberInput, Paper, Stack, Switch, Table, Text, TextInput, Title, Tooltip } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconEdit, IconPlus, IconSchool } from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { api, type AreaDto, type SubjectDto } from '../api/client';
import { EmptyState, ErrorAlert, Loading } from '../components/PageState';

type AreaTarget = { area?: AreaDto };
type SubjectTarget = { area: AreaDto; subject?: SubjectDto };

/** Catálogo institucional: áreas de conocimiento y sus asignaturas (materias). */
export function SubjectsPage() {
  const areas = useQuery({ queryKey: ['areas'], queryFn: () => api.areas.get() });
  const [areaTarget, setAreaTarget] = useState<AreaTarget | null>(null);
  const [subjectTarget, setSubjectTarget] = useState<SubjectTarget | null>(null);

  if (areas.isLoading) return <Loading label="Cargando materias…" />;
  if (areas.error) return <ErrorAlert error={areas.error} />;

  return (
    <Stack gap="md">
      <Group justify="space-between">
        <div>
          <Title order={2}>Materias</Title>
          <Text c="dimmed" size="sm">
            Áreas de conocimiento y asignaturas. La intensidad horaria de cada grado se define en el plan de estudios.
          </Text>
        </div>
        <Button leftSection={<IconPlus size={16} />} onClick={() => setAreaTarget({})}>
          Nueva área
        </Button>
      </Group>

      {(areas.data ?? []).length === 0 && (
        <EmptyState icon={IconSchool} title="Aún no hay áreas ni materias">
          <Text c="dimmed" size="sm">
            Cree un área (por ejemplo, Matemáticas) y agregue sus asignaturas.
          </Text>
        </EmptyState>
      )}

      {(areas.data ?? []).map((area) => (
        <Paper key={area.id} withBorder>
          <Group justify="space-between" p="md" pb="xs">
            <Group gap="xs">
              <Title order={4}>{area.name}</Title>
              <Badge variant="light" color="gray">
                {area.subjects?.length ?? 0} {area.subjects?.length === 1 ? 'materia' : 'materias'}
              </Badge>
              <Tooltip label="Editar área">
                <ActionIcon variant="subtle" aria-label={`Editar área ${area.name}`} onClick={() => setAreaTarget({ area })}>
                  <IconEdit size={16} />
                </ActionIcon>
              </Tooltip>
            </Group>
            <Button size="xs" variant="light" leftSection={<IconPlus size={14} />} onClick={() => setSubjectTarget({ area })}>
              Agregar materia
            </Button>
          </Group>
          <Table verticalSpacing="xs">
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Materia</Table.Th>
                <Table.Th w={110}>Abreviatura</Table.Th>
                <Table.Th w={110}>Estado</Table.Th>
                <Table.Th w={50} />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {(area.subjects ?? []).map((subject) => (
                <Table.Tr key={subject.id} c={subject.isActive ? undefined : 'dimmed'}>
                  <Table.Td fw={500}>{subject.name}</Table.Td>
                  <Table.Td>{subject.code ?? '—'}</Table.Td>
                  <Table.Td>
                    <Badge variant="light" color={subject.isActive ? 'green' : 'gray'}>
                      {subject.isActive ? 'Activa' : 'Inactiva'}
                    </Badge>
                  </Table.Td>
                  <Table.Td>
                    <Tooltip label="Editar materia">
                      <ActionIcon variant="subtle" aria-label={`Editar ${subject.name}`} onClick={() => setSubjectTarget({ area, subject })}>
                        <IconEdit size={16} />
                      </ActionIcon>
                    </Tooltip>
                  </Table.Td>
                </Table.Tr>
              ))}
              {(area.subjects ?? []).length === 0 && (
                <Table.Tr>
                  <Table.Td colSpan={4}>
                    <Text size="sm" c="dimmed">
                      Esta área aún no tiene materias.
                    </Text>
                  </Table.Td>
                </Table.Tr>
              )}
            </Table.Tbody>
          </Table>
        </Paper>
      ))}

      {areaTarget && <AreaModal target={areaTarget} onClose={() => setAreaTarget(null)} />}
      {subjectTarget && <SubjectModal target={subjectTarget} onClose={() => setSubjectTarget(null)} />}
    </Stack>
  );
}

function AreaModal({ target, onClose }: { target: AreaTarget; onClose: () => void }) {
  const queryClient = useQueryClient();
  const area = target.area;
  const [name, setName] = useState(area?.name ?? '');
  const [order, setOrder] = useState<number | string>(area?.order ?? '');

  const save = useMutation({
    mutationFn: async () => {
      const body = { name: name.trim(), order: order === '' ? null : Number(order) };
      if (area) await api.areas.byAreaId(area.id!).put(body);
      else await api.areas.post(body);
    },
    onSuccess: () => {
      notifications.show({ color: 'green', message: area ? 'Área actualizada' : 'Área creada' });
      queryClient.invalidateQueries({ queryKey: ['areas'] });
      onClose();
    }
  });

  return (
    <Modal opened onClose={onClose} title={area ? 'Editar área' : 'Nueva área'}>
      <Stack>
        {save.error && <ErrorAlert error={save.error} title="No se pudo guardar" />}
        <TextInput label="Nombre del área" required data-autofocus value={name} onChange={(e) => setName(e.currentTarget.value)} />
        <NumberInput
          label="Orden de presentación"
          description="Opcional; las áreas se muestran de menor a mayor"
          min={0}
          allowDecimal={false}
          value={order}
          onChange={setOrder}
        />
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            Cancelar
          </Button>
          <Button loading={save.isPending} disabled={!name.trim()} onClick={() => save.mutate()}>
            Guardar
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
}

function SubjectModal({ target, onClose }: { target: SubjectTarget; onClose: () => void }) {
  const queryClient = useQueryClient();
  const { area, subject } = target;
  const [name, setName] = useState(subject?.name ?? '');
  const [code, setCode] = useState(subject?.code ?? '');
  const [order, setOrder] = useState<number | string>(subject?.order ?? '');
  const [isActive, setIsActive] = useState(subject?.isActive ?? true);

  const save = useMutation({
    mutationFn: async () => {
      if (subject)
        await api.areas.byAreaId(area.id!).subjects.bySubjectId(subject.id!).put({
          name: name.trim(),
          code: code.trim() || null,
          order: order === '' ? subject.order : Number(order),
          isActive
        });
      else await api.areas.byAreaId(area.id!).subjects.post({ name: name.trim(), code: code.trim() || null, order: order === '' ? null : Number(order) });
    },
    onSuccess: () => {
      notifications.show({ color: 'green', message: subject ? 'Materia actualizada' : 'Materia creada' });
      queryClient.invalidateQueries({ queryKey: ['areas'] });
      onClose();
    }
  });

  return (
    <Modal opened onClose={onClose} title={subject ? `Editar ${subject.name}` : `Nueva materia en ${area.name}`}>
      <Stack>
        {save.error && <ErrorAlert error={save.error} title="No se pudo guardar" />}
        <TextInput label="Nombre de la materia" required data-autofocus value={name} onChange={(e) => setName(e.currentTarget.value)} />
        <TextInput label="Abreviatura" description="Opcional, por ejemplo MAT" value={code} onChange={(e) => setCode(e.currentTarget.value)} />
        <NumberInput label="Orden dentro del área" min={0} allowDecimal={false} value={order} onChange={setOrder} />
        {subject && (
          <Switch
            label="Materia activa"
            description="Las inactivas no se pueden agregar a planes nuevos; los planes anteriores las conservan"
            checked={isActive}
            onChange={(e) => setIsActive(e.currentTarget.checked)}
          />
        )}
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            Cancelar
          </Button>
          <Button loading={save.isPending} disabled={!name.trim()} onClick={() => save.mutate()}>
            Guardar
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
}
