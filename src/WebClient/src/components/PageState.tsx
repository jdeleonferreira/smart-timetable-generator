import { Alert, Center, Loader, Paper, Stack, Text, ThemeIcon, Title } from '@mantine/core';
import { IconAlertCircle, type Icon } from '@tabler/icons-react';
import type { ReactNode } from 'react';
import { errorMessage } from '../lib/errors';

export function Loading({ label = 'Cargando…' }: { label?: string }) {
  return (
    <Center py="xl">
      <Stack align="center" gap="xs">
        <Loader />
        <Text size="sm" c="dimmed">
          {label}
        </Text>
      </Stack>
    </Center>
  );
}

export function ErrorAlert({ error, title = 'No se pudo cargar' }: { error: unknown; title?: string }) {
  return (
    <Alert color="red" icon={<IconAlertCircle size={18} />} title={title} role="alert">
      {errorMessage(error)}
    </Alert>
  );
}

export function EmptyState({ icon: IconComponent, title, children }: { icon: Icon; title: string; children?: ReactNode }) {
  return (
    <Paper withBorder p="xl">
      <Stack align="center" gap="sm" py="md">
        <ThemeIcon size={48} radius="xl" variant="light">
          <IconComponent size={26} />
        </ThemeIcon>
        <Title order={4}>{title}</Title>
        {children}
      </Stack>
    </Paper>
  );
}
