import { AppShell, Avatar, Badge, Burger, Group, Menu, NavLink, Select, Stack, Text, ThemeIcon, Title, UnstyledButton } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { IconBook2, IconCalendarTime, IconChevronDown, IconLogout, IconTable, IconUsers } from '@tabler/icons-react';
import { NavLink as RouterNavLink, Outlet, useLocation } from 'react-router';
import { useAuth } from '../auth/AuthContext';
import { useSchool } from '../context/SchoolContext';
import { ROLE_LABELS } from '../lib/labels';

function initials(name?: string | null) {
  return (name ?? '?')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join('');
}

export function AppLayout() {
  const [opened, { toggle, close }] = useDisclosure();
  const { user, logout, isAdmin } = useAuth();
  const school = useSchool();
  const location = useLocation();

  const links = [
    { to: '/plan', label: 'Plan de estudios', icon: IconBook2 },
    { to: '/horarios', label: 'Horarios', icon: IconTable },
    ...(isAdmin ? [{ to: '/usuarios', label: 'Usuarios', icon: IconUsers }] : [])
  ];

  return (
    <AppShell
      header={{ height: 60 }}
      navbar={{ width: 240, breakpoint: 'sm', collapsed: { mobile: !opened } }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between" wrap="nowrap">
          <Group gap="sm" wrap="nowrap">
            <Burger opened={opened} onClick={toggle} hiddenFrom="sm" size="sm" aria-label="Menú" />
            <ThemeIcon size={34} radius="md">
              <IconCalendarTime size={20} />
            </ThemeIcon>
            <Title order={4} visibleFrom="xs">
              Horarios escolares
            </Title>
          </Group>

          <Group gap="sm" wrap="nowrap">
            <Select
              aria-label="Año lectivo"
              size="xs"
              w={110}
              data={school.years.map((y) => ({ value: y.id ?? '', label: String(y.year) }))}
              value={school.year?.id ?? null}
              onChange={(v) => v && school.setYearId(v)}
              allowDeselect={false}
              visibleFrom="xs"
            />
            <Select
              aria-label="Sede"
              size="xs"
              w={170}
              data={school.campuses.map((c) => ({ value: c.id ?? '', label: c.name ?? '' }))}
              value={school.campus?.id ?? null}
              onChange={(v) => v && school.setCampusId(v)}
              allowDeselect={false}
              visibleFrom="sm"
            />
            <Menu position="bottom-end" width={220}>
              <Menu.Target>
                <UnstyledButton aria-label="Usuario">
                  <Group gap={8} wrap="nowrap">
                    <Avatar color="blue" radius="xl" size={32}>
                      {initials(user?.fullName)}
                    </Avatar>
                    <Stack gap={0} visibleFrom="md">
                      <Text size="sm" fw={600} lh={1.2}>
                        {user?.fullName}
                      </Text>
                      <Text size="xs" c="dimmed" lh={1.2}>
                        {ROLE_LABELS[user?.role ?? ''] ?? user?.role}
                      </Text>
                    </Stack>
                    <IconChevronDown size={14} />
                  </Group>
                </UnstyledButton>
              </Menu.Target>
              <Menu.Dropdown>
                <Menu.Label>{user?.email}</Menu.Label>
                <Menu.Item leftSection={<IconLogout size={16} />} color="red" onClick={logout}>
                  Cerrar sesión
                </Menu.Item>
              </Menu.Dropdown>
            </Menu>
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="sm">
        <Stack gap={4}>
          {links.map((link) => (
            <NavLink
              key={link.to}
              component={RouterNavLink}
              to={link.to}
              label={link.label}
              leftSection={<link.icon size={18} />}
              active={location.pathname.startsWith(link.to)}
              onClick={close}
            />
          ))}
        </Stack>
        <Stack gap={6} mt="auto" hiddenFrom="sm">
          <Select
            label="Año lectivo"
            size="xs"
            data={school.years.map((y) => ({ value: y.id ?? '', label: String(y.year) }))}
            value={school.year?.id ?? null}
            onChange={(v) => v && school.setYearId(v)}
            allowDeselect={false}
          />
          <Select
            label="Sede"
            size="xs"
            data={school.campuses.map((c) => ({ value: c.id ?? '', label: c.name ?? '' }))}
            value={school.campus?.id ?? null}
            onChange={(v) => v && school.setCampusId(v)}
            allowDeselect={false}
          />
        </Stack>
        {user?.role === 'Coordinador' && school.campus && user.campusId !== school.campus.id && (
          <Badge color="yellow" variant="light" mt="sm" fullWidth>
            Solo consulta en esta sede
          </Badge>
        )}
      </AppShell.Navbar>

      <AppShell.Main bg="gray.0">
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
