import { useState } from 'react';
import {
  Center,
  Paper,
  Title,
  Text,
  TextInput,
  PasswordInput,
  Button,
  Stack,
  Alert,
  Box,
  useMantineColorScheme,
  ActionIcon,
  Tooltip,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { IconAlertCircle, IconSun, IconMoon } from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();

  const form = useForm({
    initialValues: { login: '', password: '' },
    validate: {
      login: (v) => (!v ? 'Введите логин' : null),
      password: (v) => (!v ? 'Введите пароль' : null),
    },
  });

  const handleSubmit = async (values: { login: string; password: string }) => {
    setError('');
    setLoading(true);
    try {
      await login(values);
      navigate('/');
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Ошибка входа');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Box p="md" style={{ display: 'flex', justifyContent: 'flex-end' }}>
        <Tooltip label={colorScheme === 'dark' ? 'Светлая тема' : 'Тёмная тема'}>
          <ActionIcon variant="subtle" color="gray" onClick={toggleColorScheme}>
            {colorScheme === 'dark' ? <IconSun size={18} /> : <IconMoon size={18} />}
          </ActionIcon>
        </Tooltip>
      </Box>

      <Center style={{ flex: 1 }}>
        <Paper w={380} p={36} withBorder shadow="sm" radius="md">
          <Stack gap="xs" mb="xl">
            <Title order={2} fw={700} c="indigo">
              SuspenseManager
            </Title>
            <Text size="sm" c="dimmed">
              Система обработки суспенсов
            </Text>
          </Stack>

          {error && (
            <Alert icon={<IconAlertCircle size={16} />} color="red" mb="md" radius="md">
              {error}
            </Alert>
          )}

          <form onSubmit={form.onSubmit(handleSubmit)}>
            <Stack gap="md">
              <TextInput
                label="Логин"
                placeholder="Введите логин"
                autoComplete="username"
                {...form.getInputProps('login')}
              />
              <PasswordInput
                label="Пароль"
                placeholder="Введите пароль"
                autoComplete="current-password"
                {...form.getInputProps('password')}
              />
              <Button type="submit" loading={loading} fullWidth mt="xs" color="indigo">
                Войти
              </Button>
            </Stack>
          </form>
        </Paper>
      </Center>
    </Box>
  );
}
