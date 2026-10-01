import { Alert, Anchor, Box, Button, Center, Paper, PasswordInput, Stack, Text, TextInput, ThemeIcon, Title } from '@mantine/core';
import { IconAlertCircle, IconCalendarTime } from '@tabler/icons-react';
import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../lib/errors';

const SAMPLE_USERS = [
  { email: 'admin@colegio.local', password: 'Admin2026', label: 'Administrador' },
  { email: 'coordinador@colegio.local', password: 'Coordinador2026', label: 'Coordinador' },
  { email: 'docente@colegio.local', password: 'Docente2026', label: 'Docente' }
];

export function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const from = (location.state as { from?: string } | null)?.from ?? '/';
  if (user) return <Navigate to={from} replace />;

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(email.trim(), password);
      navigate(from, { replace: true });
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Center mih="100vh" bg="gray.0" p="md">
      <Box w="100%" maw={420}>
        <Stack align="center" gap={6} mb="lg">
          <ThemeIcon size={52} radius="md" variant="filled">
            <IconCalendarTime size={30} />
          </ThemeIcon>
          <Title order={2}>Horarios escolares</Title>
          <Text c="dimmed" size="sm">
            Plan de estudios y horarios de la institución
          </Text>
        </Stack>

        <Paper withBorder shadow="sm" p="xl">
          <form onSubmit={submit}>
            <Stack>
              {error && (
                <Alert color="red" icon={<IconAlertCircle size={18} />} role="alert">
                  {error}
                </Alert>
              )}
              <TextInput
                label="Correo"
                placeholder="usuario@colegio.edu.co"
                type="email"
                autoComplete="username"
                required
                value={email}
                onChange={(e) => setEmail(e.currentTarget.value)}
              />
              <PasswordInput
                label="Contraseña"
                autoComplete="current-password"
                required
                value={password}
                onChange={(e) => setPassword(e.currentTarget.value)}
              />
              <Button type="submit" loading={submitting} fullWidth mt="xs">
                Ingresar
              </Button>
            </Stack>
          </form>
        </Paper>

        {import.meta.env.DEV && (
          <Paper withBorder p="sm" mt="md" bg="blue.0">
            <Text size="xs" fw={600} mb={4}>
              Usuarios de ejemplo (desarrollo)
            </Text>
            <Stack gap={2}>
              {SAMPLE_USERS.map((u) => (
                <Anchor
                  key={u.email}
                  size="xs"
                  component="button"
                  type="button"
                  ta="left"
                  onClick={() => {
                    setEmail(u.email);
                    setPassword(u.password);
                  }}
                >
                  {u.label}: {u.email} / {u.password}
                </Anchor>
              ))}
            </Stack>
          </Paper>
        )}
      </Box>
    </Center>
  );
}
