import { AppShell, Avatar, Badge, Burger, Group, Menu, NavLink, Select, Stack, Text, ThemeIcon, Title, UnstyledButton } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { IconBook2, IconCalendarOff, IconCalendarTime, IconChevronDown, IconLogout, IconSchool, IconTable, IconUserCheck, IconUsers, IconUsersGroup } from '@tabler/icons-react';
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
  const { user, logout, isAdmin, isManager } = useAuth();
  const school = useSchool();
  const location = useLocation();

  const links = [
    { to: '/plan', label: 'Plan de estudios', icon: IconBook2 },
    { to: '/asignacion', label: 'Asignación de docentes', icon: IconUserCheck },
    ...(isManager ? [{ to: '/docentes', label: 'Docentes', icon: IconUsersGroup }] : []),
    ...(user?.teacherId ? [{ to: '/disponibilidad', label: 'Mi disponibilidad', icon: IconCalendarOff }] : []),
    { to: '/horarios', label: 'Horarios', icon: IconTable }
  ];
  const adminLinks = [
    { to: '/materias', label: 'Materias', icon: IconSchool },
    { to: '/usuarios', label: 'Usuarios', icon: IconUsers }
  ];

  return (
    <AppShell
      header={{ height: 60 }}
      navbar={{ width: 240, breakpoint: 'sm', collapsed: { mobile: !opened } }}
      padding="md"
    >
      <AppShell.Header className="app-header">
        <Group h="100%" px="md" justify="space-between" wrap="nowrap">
          <Group gap="sm" wrap="nowrap">
            <Burger opened={opened} onClick={toggle} hiddenFrom="sm" size="sm" color="white" aria-label="Menú" />
            <ThemeIcon size={36} radius="md" color="gold" variant="filled">
              <IconCalendarTime size={21} />
            </ThemeIcon>
            <Stack gap={0}>
              <Title order={4} lh={1.15}>
                Horarios escolares
              </Title>
              <Text size="xs" className="brand-sub" visibleFrom="xs" lh={1.2} opacity={0.75}>
                Plan de estudios y horarios institucionales
              </Text>
            </Stack>
          </Group>

          <Group gap="sm" wrap="nowrap">
            <Select
              aria-label="Año lectivo"
              className="header-select"
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
              className="header-select"
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
                    <Avatar color="gold" variant="filled" radius="xl" size={34}>
                      {initials(user?.fullName)}
                    </Avatar>
                    <Stack gap={0} visibleFrom="md">
                      <Text size="sm" fw={600} lh={1.2} className="user-name">
                        {user?.fullName}
                      </Text>
                      <Text size="xs" lh={1.2} className="user-role">
                        {ROLE_LABELS[user?.role ?? ''] ?? user?.role}
                      </Text>
                    </Stack>
                    <IconChevronDown size={14} color="white" />
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

      <AppShell.Navbar p="sm" className="app-navbar">
        <Text className="nav-caption" mt={4} mb={4}>
          Gestión académica
        </Text>
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
        {isAdmin && (
          <>
            <Text className="nav-caption" mt="md" mb={4}>
              Administración
            </Text>
            <Stack gap={4}>
              {adminLinks.map((link) => (
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
          </>
        )}
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

      <AppShell.Main style={{ background: 'var(--app-bg)' }}>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
