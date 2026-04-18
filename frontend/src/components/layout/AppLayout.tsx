import React, { useState } from 'react';
import { AppShell, Burger, Group, Text, NavLink, ActionIcon, Tooltip, Avatar, Menu, Divider, useMantineColorScheme, useMantineTheme } from '@mantine/core';
import { useNavigate, useLocation, Outlet } from 'react-router-dom';
import {
  IconDashboard,
  IconUpload,
  IconLayersSubtract,
  IconFolderOpen,
  IconClock,
  IconList,
  IconLogout,
  IconSun,
  IconMoon,
  IconChevronRight,
  IconHistory,
  IconBriefcase,
  IconBook2,
} from '@tabler/icons-react';
import { useAuth } from '../../hooks/useAuth';

const NAV_ITEMS = [
  { path: '/', label: 'Дашборд', icon: IconDashboard },
  { path: '/upload', label: 'Загрузка', icon: IconUpload },
  { path: '/grouping', label: 'Группировка', icon: IconLayersSubtract },
  { path: '/groups', label: 'Сохранённые группы', icon: IconFolderOpen },
  { path: '/postponed', label: 'Отложенные', icon: IconClock },
  { path: '/suspenses', label: 'Сасп. строки', icon: IconList },
  { path: '/audit', label: 'Аудит', icon: IconHistory },
  { path: '/backoffice/tasks', label: 'Бэк-офис', icon: IconBriefcase },
  { path: '/catalog', label: 'Каталог', icon: IconBook2 },
];

export function AppLayout() {
  const [opened, setOpened] = useState(false);
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();
  const theme = useMantineTheme();
  const navigate = useNavigate();
  const location = useLocation();
  const { loginName, logout } = useAuth();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <AppShell
      header={{ height: 58 }}
      navbar={{ width: 230, breakpoint: 'sm', collapsed: { mobile: !opened } }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Group>
            <Burger opened={opened} onClick={() => setOpened((o) => !o)} hiddenFrom="sm" size="sm" />
            <Text
              fw={700}
              size="lg"
              c="indigo"
              style={{ letterSpacing: '-0.5px', cursor: 'pointer' }}
              onClick={() => navigate('/')}
            >
              SuspenseManager
            </Text>
          </Group>

          <Group gap="xs">
            <Tooltip label={colorScheme === 'dark' ? 'Светлая тема' : 'Тёмная тема'}>
              <ActionIcon variant="subtle" color="gray" onClick={toggleColorScheme}>
                {colorScheme === 'dark' ? <IconSun size={18} /> : <IconMoon size={18} />}
              </ActionIcon>
            </Tooltip>

            <Menu shadow="md" width={180}>
              <Menu.Target>
                <Group gap="xs" style={{ cursor: 'pointer' }}>
                  <Avatar size={30} color="indigo" radius="xl">
                    {loginName.slice(0, 2).toUpperCase() || 'SM'}
                  </Avatar>
                  <Text size="sm" visibleFrom="sm">{loginName}</Text>
                </Group>
              </Menu.Target>
              <Menu.Dropdown>
                <Menu.Label>{loginName}</Menu.Label>
                <Divider />
                <Menu.Item leftSection={<IconLogout size={14} />} color="red" onClick={handleLogout}>
                  Выйти
                </Menu.Item>
              </Menu.Dropdown>
            </Menu>
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="xs">
        {NAV_ITEMS.map(({ path, label, icon: Icon }) => {
          const active = path === '/'
            ? location.pathname === '/'
            : location.pathname.startsWith(path);
          return (
            <NavLink
              key={path}
              label={label}
              leftSection={<Icon size={16} />}
              rightSection={active ? <IconChevronRight size={12} /> : null}
              active={active}
              onClick={() => { navigate(path); setOpened(false); }}
              style={{ borderRadius: theme.radius.sm }}
              mb={2}
            />
          );
        })}
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
