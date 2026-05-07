import { useEffect } from 'react';
import {
  Stack,
  Title,
  Text,
  Paper,
  Textarea,
  Button,
  Group,
  Select,
  Switch,
  Skeleton,
  Alert,
  Divider,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconAlertCircle, IconDeviceFloppy } from '@tabler/icons-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateMeSettings } from '../api/me';
import { useMeSettingsQuery, ME_SETTINGS_QUERY_KEY } from '../hooks/useMeSettings';
import type { ColorSchemePreference, NotificationsPositionPreference, UpdateMeSettingsDto } from '../types';
import { useThemePreference } from '../theme/ThemePreferenceContext';

const PAGE_SIZE_OPTIONS = ['2', '5', '10', '15', '20', '50', '100'].map((v) => ({ value: v, label: v }));

const COLOR_SCHEME_OPTIONS: { value: ColorSchemePreference; label: string }[] = [
  { value: 'light', label: 'Светлая' },
  { value: 'dark', label: 'Тёмная' },
  { value: 'auto', label: 'Как в системе' },
];

const NOTIFICATIONS_POSITION_OPTIONS: { value: NotificationsPositionPreference; label: string }[] = [
  { value: 'top-right', label: 'Сверху справа' },
  { value: 'top-left', label: 'Сверху слева' },
  { value: 'top-center', label: 'Сверху по центру' },
  { value: 'bottom-right', label: 'Снизу справа' },
  { value: 'bottom-left', label: 'Снизу слева' },
  { value: 'bottom-center', label: 'Снизу по центру' },
];

export function SettingsPage() {
  const queryClient = useQueryClient();
  const { setPreference } = useThemePreference();
  const { data, isLoading, error, isFetching } = useMeSettingsQuery();

  const form = useForm({
    initialValues: {
      description: '',
      defaultTablePageSize: 20,
      filtersExpandedByDefault: false,
      colorScheme: 'light' as ColorSchemePreference,
      notificationsPosition: 'top-right' as NotificationsPositionPreference,
    },
  });

  useEffect(() => {
    if (!data) return;
    form.setValues({
      description: data.description ?? '',
      defaultTablePageSize: data.preferences.defaultTablePageSize,
      filtersExpandedByDefault: data.preferences.filtersExpandedByDefault,
      colorScheme: data.preferences.colorScheme,
      notificationsPosition: data.preferences.notificationsPosition,
    });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data]);

  const saveMutation = useMutation({
    mutationFn: (dto: UpdateMeSettingsDto) => updateMeSettings(dto),
    onMutate: async () => {
      // Отменяем фоновые рефетчи, чтобы они не перезаписали только что сохранённые данные.
      await queryClient.cancelQueries({ queryKey: ME_SETTINGS_QUERY_KEY });
    },
    onSuccess: (updated) => {
      queryClient.setQueryData(ME_SETTINGS_QUERY_KEY, updated);
      setPreference(updated.preferences.colorScheme as ColorSchemePreference);
      notifications.show({ color: 'green', title: 'Сохранено', message: 'Настройки обновлены' });
    },
    onError: (e: Error) => {
      notifications.show({ color: 'red', title: 'Ошибка', message: e.message });
    },
  });

  const handleSubmit = form.onSubmit((values) => {
    const dto: UpdateMeSettingsDto = {
      description: values.description.trim(),
      preferences: {
        defaultTablePageSize: values.defaultTablePageSize,
        filtersExpandedByDefault: values.filtersExpandedByDefault,
        colorScheme: values.colorScheme,
        notificationsPosition: values.notificationsPosition,
      },
    };
    saveMutation.mutate(dto);
  });

  const busy = isLoading || isFetching;

  return (
    <Stack gap="lg" maw={720}>
      <div>
        <Title order={3} fw={600}>Настройки</Title>
        <Text c="dimmed" size="sm">
          Общие параметры интерфейса хранятся на сервере и применяются после входа.
        </Text>
      </div>

      {error && (
        <Alert icon={<IconAlertCircle size={16} />} color="red">
          {(error as Error).message}
        </Alert>
      )}

      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <Paper withBorder radius="md" p="md">
            <Text fw={600} mb="sm">Аккаунт</Text>
            {busy && !data ? (
              <Skeleton height={80} />
            ) : (
              <Stack gap="sm">
                <Text size="sm" c="dimmed">Логин: <strong>{data?.login ?? '—'}</strong></Text>
                <Textarea
                  label="Заметка к аккаунту"
                  description="Внутренняя пометка (до 1000 символов), видна только в настройках и админке."
                  minRows={2}
                  maxLength={1000}
                  key={form.key('description')}
                  {...form.getInputProps('description')}
                />
              </Stack>
            )}
          </Paper>

          <Paper withBorder radius="md" p="md">
            <Text fw={600} mb="xs">Интерфейс</Text>
            <Text size="sm" c="dimmed" mb="md">
              Универсальные настройки для всех экранов.
            </Text>
            {busy && !data ? (
              <Skeleton height={260} />
            ) : (
              <Stack gap="md">
                <Select
                  label="Размер страницы в таблицах"
                  description="Подставляется при открытии списков (суспенсы, аудит, группы и т.д.)."
                  data={PAGE_SIZE_OPTIONS}
                  value={String(form.values.defaultTablePageSize)}
                  onChange={(v) => form.setFieldValue('defaultTablePageSize', v ? Number(v) : 20)}
                  allowDeselect={false}
                />

                <Select
                  label="Тема"
                  description="Сохраняется в профиле; переключатель в шапке тоже обновляет это значение."
                  data={COLOR_SCHEME_OPTIONS}
                  value={form.values.colorScheme}
                  onChange={(v) =>
                    form.setFieldValue('colorScheme', (v as ColorSchemePreference) ?? 'light')}
                  allowDeselect={false}
                />

                <Select
                  label="Позиция уведомлений"
                  description="Куда выводить всплывающие сообщения об ошибках и успешных действиях."
                  data={NOTIFICATIONS_POSITION_OPTIONS}
                  value={form.values.notificationsPosition}
                  onChange={(v) =>
                    form.setFieldValue(
                      'notificationsPosition',
                      (v as NotificationsPositionPreference) ?? 'top-right',
                    )}
                  allowDeselect={false}
                />

                <Divider label="Фильтры" labelPosition="left" />

                <Switch
                  label="Раскрывать фильтры по умолчанию"
                  description="Панель фильтров будет открыта при входе на страницу."
                  key={form.key('filtersExpandedByDefault')}
                  {...form.getInputProps('filtersExpandedByDefault', { type: 'checkbox' })}
                />
              </Stack>
            )}
          </Paper>

          <Group>
            <Button
              type="submit"
              leftSection={<IconDeviceFloppy size={18} />}
              loading={saveMutation.isPending}
              disabled={busy && !data}
            >
              Сохранить
            </Button>
          </Group>
        </Stack>
      </form>
    </Stack>
  );
}
