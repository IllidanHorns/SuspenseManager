import { Alert, Center, Group, Loader, Modal, Stack, Text } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { getAccountById } from '../../api/accounts';
import { fmtDateTime } from '../../utils/format';

interface UserProfileModalProps {
  opened: boolean;
  onClose: () => void;
  accountId: number | null;
}

function value(v: string | number | null | undefined) {
  return v === null || v === undefined || v === '' ? '—' : String(v);
}

export function UserProfileModal({ opened, onClose, accountId }: UserProfileModalProps) {
  const { data, isLoading, error } = useQuery({
    queryKey: ['account-profile', accountId],
    queryFn: () => getAccountById(accountId!),
    enabled: opened && !!accountId,
  });

  return (
    <Modal opened={opened} onClose={onClose} title="Детальная информация о пользователе" centered size="lg">
      {isLoading ? (
        <Center py="xl"><Loader color="indigo" /></Center>
      ) : error ? (
        <Alert icon={<IconAlertCircle size={16} />} color="red">
          {(error as Error).message}
        </Alert>
      ) : !data ? (
        <Text c="dimmed">Нет данных</Text>
      ) : (
        <Stack gap="xs">
          <Text fw={600}>Аккаунт</Text>
          <Group justify="space-between"><Text c="dimmed">Логин</Text><Text>{value(data.login)}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Описание</Text><Text>{value(data.description)}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Создан</Text><Text>{value(fmtDateTime(data.createTime))}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Обновлен</Text><Text>{value(fmtDateTime(data.changeTime))}</Text></Group>

          <Text fw={600} mt="sm">Пользователь</Text>
          <Group justify="space-between"><Text c="dimmed">ФИО</Text><Text>{data.user ? `${data.user.surname} ${data.user.name} ${data.user.middleName ?? ''}`.trim() : '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Email</Text><Text>{value(data.user?.email)}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Телефон</Text><Text>{value(data.user?.phoneNumber)}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Должность</Text><Text>{value(data.user?.position)}</Text></Group>

        </Stack>
      )}
    </Modal>
  );
}
