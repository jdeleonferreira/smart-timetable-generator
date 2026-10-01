import { Alert, Anchor, Box, Button, Center, Flex, Group, Paper, PasswordInput, Stack, Text, TextInput, ThemeIcon, Title } from '@mantine/core';
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
    <Flex mih="100vh">
      <Stack className="login-hero" justify="space-between" p={56} w="42%" visibleFrom="md">
        <Group gap="sm">
          <ThemeIcon size={44} radius="md" color="gold" variant="filled">
            <IconCalendarTime size={26} />
          </ThemeIcon>
          <Text fw={600} size="lg" ff="Georgia, serif">
            Institución educativa
          </Text>
        </Group>
        <Stack gap="md" maw={420}>
          <div className="hero-rule" />
          <Title order={1} fz={38} lh={1.2}>
            Gestión de horarios escolares
          </Title>
          <Text c="navy.1" size="md" lh={1.6}>
            Planifica el plan de estudios, genera los horarios de cada curso y consulta la carga de cada docente en un solo lugar.
          </Text>
        </Stack>
        <Text size="xs" c="navy.2">
          © {new Date().getFullYear()} Horarios escolares
        </Text>
      </Stack>

      <Center flex={1} bg="gray.0" p="md">
        <Box w="100%" maw={400}>
          <Stack align="center" gap={6} mb="lg" hiddenFrom="md">
            <ThemeIcon size={52} radius="md" variant="filled">
              <IconCalendarTime size={30} />
            </ThemeIcon>
          </Stack>
          <Stack gap={4} mb="lg">
            <Title order={2}>Horarios escolares</Title>
            <Text c="dimmed" size="sm">
              Inicia sesión con tu cuenta institucional
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
                <Button type="submit" loading={submitting} fullWidth mt="xs" size="md">
                  Ingresar
                </Button>
              </Stack>
            </form>
          </Paper>

          {import.meta.env.DEV && (
            <Paper withBorder p="sm" mt="md" bg="navy.0">
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
    </Flex>
  );
}
