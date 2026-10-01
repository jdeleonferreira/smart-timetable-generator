import { Alert, Stack, Text, Title } from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { ErrorAlert, Loading } from '../components/PageState';
import { useSchool } from '../context/SchoolContext';
import { AvailabilityGrid } from '../features/teachers/AvailabilityGrid';

/** El docente marca las horas en que no puede o prefiere no dictar clase; el generador las tiene en cuenta. */
export function MyAvailabilityPage() {
  const { user } = useAuth();
  const { campuses } = useSchool();
  const teachers = useQuery({ queryKey: ['teachers'], queryFn: () => api.catalog.teachers.get() });

  if (teachers.isLoading) return <Loading label="Cargando su disponibilidad…" />;
  if (teachers.error) return <ErrorAlert error={teachers.error} />;

  const teacher = teachers.data?.find((t) => t.id === user?.teacherId);
  if (!teacher)
    return (
      <Alert color="yellow" icon={<IconInfoCircle size={18} />} title="Su cuenta no está vinculada a un docente">
        Pida al administrador que vincule su usuario a su registro de docente.
      </Alert>
    );

  return (
    <Stack gap="md">
      <div>
        <Title order={2}>Mi disponibilidad</Title>
        <Text c="dimmed" size="sm">
          Marque las horas en que no puede dictar clase y las que prefiere evitar. El horario se genera respetando estas marcas.
        </Text>
      </div>
      <AvailabilityGrid teacher={teacher} campuses={campuses} editable />
    </Stack>
  );
}
