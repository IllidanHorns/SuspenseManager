import { Alert, Center, Group, Loader, Modal, Paper, SimpleGrid, Stack, Text } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { BarChart, DonutChart } from '@mantine/charts';
import { getAccountById } from '../../api/accounts';
import { getAuditGroups, getAuditLines } from '../../api/audit';
import { fmtDateTime } from '../../utils/format';

interface MyProfileModalProps {
  opened: boolean;
  onClose: () => void;
  accountId: number;
  loginName: string;
}

function value(v: string | null | undefined) {
  return v === null || v === undefined || v === '' ? '—' : v;
}

export function MyProfileModal({ opened, onClose, accountId, loginName }: MyProfileModalProps) {
  const { data: account, isLoading: accountLoading, error: accountError } = useQuery({
    queryKey: ['my-account-profile', accountId],
    queryFn: () => getAccountById(accountId),
    enabled: opened && accountId > 0,
  });

  const { data: myGroups, isLoading: groupsLoading } = useQuery({
    queryKey: ['my-kpi-groups', accountId],
    queryFn: () => getAuditGroups({ pageNumber: 1, pageSize: 200, onlyMine: true }),
    enabled: opened && accountId > 0,
  });

  const { data: myLines, isLoading: linesLoading } = useQuery({
    queryKey: ['my-kpi-lines', accountId],
    queryFn: () => getAuditLines({ pageNumber: 1, pageSize: 200, onlyMine: true }),
    enabled: opened && accountId > 0,
  });

  const kpiLoading = groupsLoading || linesLoading;

  const groupsTotal = myGroups?.totalCount ?? 0;
  const linesTotal = myLines?.totalCount ?? 0;
  const donutData = [
    { name: 'Группы', value: groupsTotal, color: 'indigo' },
    { name: 'Строки', value: linesTotal, color: 'cyan' },
  ].filter((d) => d.value > 0);

  const today = new Date();
  const weekDays = Array.from({ length: 7 }).map((_, i) => {
    const d = new Date(today);
    d.setDate(today.getDate() - (6 - i));
    const key = d.toISOString().slice(0, 10);
    return { key, label: d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' }), groups: 0, lines: 0 };
  });

  for (const g of myGroups?.items ?? []) {
    const key = (g.lastChangeTime ?? g.createTime ?? '').slice(0, 10);
    const bucket = weekDays.find((day) => day.key === key);
    if (bucket) bucket.groups += 1;
  }
  for (const l of myLines?.items ?? []) {
    const key = (l.lastChangeTime ?? l.createTime ?? '').slice(0, 10);
    const bucket = weekDays.find((day) => day.key === key);
    if (bucket) bucket.lines += 1;
  }
  const activityData = weekDays.map((d) => ({ day: d.label, groups: d.groups, lines: d.lines }));

  return (
    <Modal opened={opened} onClose={onClose} title="Мой профиль" centered size={1100}>
      {accountError ? (
        <Alert icon={<IconAlertCircle size={16} />} color="red">
          {(accountError as Error).message}
        </Alert>
      ) : (
        <Stack gap="md">
          <Paper withBorder radius="md" p="md">
            <Text fw={600} mb="xs">Профиль</Text>
            {accountLoading ? (
              <Center py="md"><Loader size="sm" color="indigo" /></Center>
            ) : (
              <Stack gap={6}>
                <Group justify="space-between"><Text c="dimmed">Логин</Text><Text>{value(loginName)}</Text></Group>
                <Group justify="space-between"><Text c="dimmed">ФИО</Text><Text>{account?.user ? `${account.user.surname} ${account.user.name} ${account.user.middleName ?? ''}`.trim() : '—'}</Text></Group>
                <Group justify="space-between"><Text c="dimmed">Email</Text><Text>{value(account?.user?.email)}</Text></Group>
                <Group justify="space-between"><Text c="dimmed">Телефон</Text><Text>{value(account?.user?.phoneNumber)}</Text></Group>
                <Group justify="space-between"><Text c="dimmed">Должность</Text><Text>{value(account?.user?.position)}</Text></Group>
                <Group justify="space-between"><Text c="dimmed">Создан</Text><Text>{value(fmtDateTime(account?.createTime))}</Text></Group>
              </Stack>
            )}
          </Paper>

          <SimpleGrid cols={{ base: 1, md: 2 }} spacing="md">
            <Paper withBorder radius="md" p="md">
              <Text fw={600} mb="xs">Правки по типу объекта</Text>
              <Text size="sm" c="dimmed" mb="md">
                Доля записей аудита по группам и по строкам суспенсов (всего в системе с вашим участием).
              </Text>
              {kpiLoading ? (
                <Center py="md"><Loader size="sm" color="indigo" /></Center>
              ) : donutData.length === 0 ? (
                <Center py="xl">
                  <Text c="dimmed" size="sm">Пока нет правок для отображения</Text>
                </Center>
              ) : (
                <DonutChart
                  data={donutData}
                  withLabelsLine
                  withLabels
                  labelsType="value"
                  size={220}
                  thickness={40}
                  mx="auto"
                />
              )}
            </Paper>

            <Paper withBorder radius="md" p="md">
              <Text fw={600} mb="xs">Моя активность за 7 дней</Text>
              <Text size="sm" c="dimmed" mb="md">
                По дате последнего изменения в выборке до 200 последних записей аудита по группам и строкам.
              </Text>
              {kpiLoading ? (
                <Center py="md"><Loader size="sm" color="indigo" /></Center>
              ) : (
                <BarChart
                  h={240}
                  data={activityData}
                  dataKey="day"
                  series={[
                    { name: 'groups', label: 'Группы', color: 'indigo' },
                    { name: 'lines', label: 'Строки', color: 'cyan' },
                  ]}
                  tickLine="y"
                  gridAxis="y"
                />
              )}
            </Paper>
          </SimpleGrid>
        </Stack>
      )}
    </Modal>
  );
}
