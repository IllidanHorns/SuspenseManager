import { useState } from 'react';
import {
  Stack,
  Title,
  Text,
  Paper,
  Table,
  ScrollArea,
  Badge,
  Group,
  ActionIcon,
  Tooltip,
  Alert,
  Loader,
  Center,
  ThemeIcon,
} from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import {
  IconChevronDown,
  IconChevronRight,
  IconInfoCircle,
  IconAlertTriangle,
  IconAlertCircle,
} from '@tabler/icons-react';
import { getOperatorSummary, getOperatorGroups, type OperatorMonitorDto, type OperatorGroupDto } from '../api/monitor';
import { ResizableTh } from '../components/common/ResizableTh';
import { StatusBadge } from '../components/common/StatusBadge';

function FlagBadge({ level, reason }: { level: string; reason?: string }) {
  if (level === 'critical') {
    return (
      <Tooltip label={reason} disabled={!reason}>
        <Badge color="red" size="sm" variant="light" leftSection={<IconAlertCircle size={11} />}>
          Критично
        </Badge>
      </Tooltip>
    );
  }
  if (level === 'warning') {
    return (
      <Tooltip label={reason} disabled={!reason}>
        <Badge color="yellow" size="sm" variant="light" leftSection={<IconAlertTriangle size={11} />}>
          Внимание
        </Badge>
      </Tooltip>
    );
  }
  return <Badge color="teal" size="sm" variant="light">Норма</Badge>;
}

function OperatorGroupsTable({ accountId }: { accountId: number }) {
  const { data: groups, isLoading } = useQuery<OperatorGroupDto[]>({
    queryKey: ['monitor-operator-groups', accountId],
    queryFn: () => getOperatorGroups(accountId),
    staleTime: 60_000,
  });

  if (isLoading) {
    return <Center py="sm"><Loader size="xs" color="indigo" /></Center>;
  }

  if (!groups || groups.length === 0) {
    return <Text size="sm" c="dimmed" p="sm">Нет активных групп</Text>;
  }

  const rowBgOf = (level: string) =>
    level === 'critical'
      ? 'var(--mantine-color-red-light)'
      : level === 'warning'
      ? 'var(--mantine-color-yellow-light)'
      : undefined;

  return (
    <ScrollArea>
      <Table fz="sm" style={{ fontSize: 'var(--mantine-font-size-sm)' }}>
        <Table.Thead>
          <Table.Tr>
            <ResizableTh minWidth={60} style={{ paddingLeft: 48 }}>ID</ResizableTh>
            <ResizableTh minWidth={110}>Статус</ResizableTh>
            <ResizableTh minWidth={140}>Исполнитель</ResizableTh>
            <ResizableTh minWidth={160}>Название</ResizableTh>
            <ResizableTh minWidth={110}>ISRC</ResizableTh>
            <ResizableTh minWidth={70} style={{ textAlign: 'center' }}>Записей</ResizableTh>
            <ResizableTh minWidth={80} style={{ textAlign: 'center' }}>Возраст</ResizableTh>
            <ResizableTh minWidth={80} style={{ textAlign: 'center' }}>Без изм.</ResizableTh>
            <ResizableTh minWidth={130}>Флаг</ResizableTh>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {groups.map(g => (
            <Table.Tr key={g.groupId} style={{ background: rowBgOf(g.flagLevel) }}>
              <Table.Td style={{ paddingLeft: 48 }}>
                <Text size="sm" fw={600} c="indigo">#{g.groupId}</Text>
              </Table.Td>
              <Table.Td><StatusBadge status={g.businessStatus} /></Table.Td>
              <Table.Td><Text size="sm">{g.artist ?? '—'}</Text></Table.Td>
              <Table.Td><Text size="sm">{g.title ?? '—'}</Text></Table.Td>
              <Table.Td><Text size="xs" ff="monospace">{g.isrc ?? '—'}</Text></Table.Td>
              <Table.Td style={{ textAlign: 'center' }}>
                <Badge variant="light" color="blue" size="sm">{g.suspenseCount}</Badge>
              </Table.Td>
              <Table.Td style={{ textAlign: 'center' }}>
                <Text size="sm">{g.ageDays} дн.</Text>
              </Table.Td>
              <Table.Td style={{ textAlign: 'center' }}>
                <Text size="sm">{g.daysSinceLastActivity} дн.</Text>
              </Table.Td>
              <Table.Td>
                <FlagBadge level={g.flagLevel} reason={g.flagReason} />
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>
    </ScrollArea>
  );
}

function OperatorRow({ op }: { op: OperatorMonitorDto }) {
  const [expanded, setExpanded] = useState(false);

  const rowBg = op.criticalGroupsCount > 0
    ? 'var(--mantine-color-red-light)'
    : op.warningGroupsCount > 0
    ? 'var(--mantine-color-yellow-light)'
    : undefined;

  return (
    <>
      <Table.Tr
        style={{ background: rowBg, cursor: 'pointer' }}
        onClick={() => setExpanded(e => !e)}
      >
        <Table.Td style={{ width: 40 }}>
          <ActionIcon variant="subtle" size="sm" color="gray" component="span">
            {expanded ? <IconChevronDown size={14} /> : <IconChevronRight size={14} />}
          </ActionIcon>
        </Table.Td>
        <Table.Td>
          <Text size="sm" fw={500}>{op.login}</Text>
          {op.fullName && <Text size="xs" c="dimmed">{op.fullName}</Text>}
        </Table.Td>
        <Table.Td style={{ textAlign: 'center' }}>
          <Badge size="sm" color="blue" variant="light">{op.activeGroupsCount}</Badge>
        </Table.Td>
        <Table.Td style={{ textAlign: 'center' }}>
          <Badge size="sm" color="violet" variant="light">{op.postponedGroupsCount}</Badge>
        </Table.Td>
        <Table.Td style={{ textAlign: 'center' }}>
          <Badge size="sm" color="orange" variant="light">{op.backOfficeGroupsCount}</Badge>
        </Table.Td>
        <Table.Td style={{ textAlign: 'center' }}>
          <Text size="sm">{op.totalSuspensesCount.toLocaleString('ru-RU')}</Text>
        </Table.Td>
        <Table.Td style={{ textAlign: 'center' }}>
          <Text size="sm">{op.oldestGroupAgeDays} дн.</Text>
        </Table.Td>
        <Table.Td style={{ textAlign: 'center' }}>
          <Group gap={4} justify="center" wrap="nowrap">
            {op.criticalGroupsCount > 0 && (
              <Badge size="sm" color="red" variant="light" leftSection={<IconAlertCircle size={11} />}>
                {op.criticalGroupsCount}
              </Badge>
            )}
            {op.warningGroupsCount > 0 && (
              <Badge size="sm" color="yellow" variant="light" leftSection={<IconAlertTriangle size={11} />}>
                {op.warningGroupsCount}
              </Badge>
            )}
            {op.criticalGroupsCount === 0 && op.warningGroupsCount === 0 && (
              <Badge size="sm" color="teal" variant="light">Норма</Badge>
            )}
          </Group>
        </Table.Td>
      </Table.Tr>
      {expanded && (
        <Table.Tr>
          <Table.Td colSpan={8} p={0} style={{ borderBottom: '2px solid var(--mantine-color-default-border)' }}>
            <OperatorGroupsTable accountId={op.accountId} />
          </Table.Td>
        </Table.Tr>
      )}
    </>
  );
}

export function MonitorPage() {
  const { data: operators, isLoading, error } = useQuery<OperatorMonitorDto[]>({
    queryKey: ['monitor-operators'],
    queryFn: getOperatorSummary,
    staleTime: 60_000,
    retry: false,
  });

  return (
    <Stack gap="md">
      <Title order={3} fw={600}>Мониторинг операторов</Title>

      <Alert
        icon={<IconInfoCircle size={16} />}
        color="gray"
        variant="light"
        title="Правила выставления флагов"
      >
        <Group gap="xl" wrap="wrap">
          <Group gap="xs">
            <ThemeIcon size="xs" color="yellow" variant="light" radius="xl">
              <IconAlertTriangle size={10} />
            </ThemeIcon>
            <Text size="xs">
              <Text span fw={600}>Внимание:</Text>{' '}
              активная группа (15/16) без изменений ≥ 7 дн. или отложенная (30/32) ≥ 14 дн.
            </Text>
          </Group>
          <Group gap="xs">
            <ThemeIcon size="xs" color="red" variant="light" radius="xl">
              <IconAlertCircle size={10} />
            </ThemeIcon>
            <Text size="xs">
              <Text span fw={600}>Критично:</Text>{' '}
              активная группа без изменений ≥ 14 дн. или отложенная ≥ 30 дн.
            </Text>
          </Group>
          <Text size="xs" c="dimmed">
            Бэк-офис (120, 320) флагов не получает. Кликните по строке оператора, чтобы раскрыть список его групп.
          </Text>
        </Group>
      </Alert>

      {isLoading && <Center py="xl"><Loader color="indigo" /></Center>}

      {error && (
        <Alert color="red" icon={<IconAlertCircle size={16} />} title="Ошибка загрузки">
          Не удалось получить данные мониторинга
        </Alert>
      )}

      {operators && (
        <Paper withBorder radius="md">
          <ScrollArea>
            <Table striped>
              <Table.Thead>
                <Table.Tr>
                  <ResizableTh minWidth={40} style={{ width: 40 }} />
                  <ResizableTh minWidth={120}>Оператор</ResizableTh>
                  <ResizableTh minWidth={90} style={{ textAlign: 'center' }}>
                    <Tooltip label="Статусы 15, 16 — в работе">
                      <span>Активные</span>
                    </Tooltip>
                  </ResizableTh>
                  <ResizableTh minWidth={100} style={{ textAlign: 'center' }}>
                    <Tooltip label="Статусы 30, 32 — отложены">
                      <span>Отложенные</span>
                    </Tooltip>
                  </ResizableTh>
                  <ResizableTh minWidth={95} style={{ textAlign: 'center' }}>
                    <Tooltip label="Статусы 120, 320 — в бэк-офисе">
                      <span>Бэк-офис</span>
                    </Tooltip>
                  </ResizableTh>
                  <ResizableTh minWidth={80} style={{ textAlign: 'center' }}>Записей</ResizableTh>
                  <ResizableTh minWidth={100} style={{ textAlign: 'center' }}>
                    <Tooltip label="Возраст самой старой группы">
                      <span>Макс. возраст</span>
                    </Tooltip>
                  </ResizableTh>
                  <ResizableTh minWidth={130} style={{ textAlign: 'center' }}>Флаги</ResizableTh>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {operators.length === 0 ? (
                  <Table.Tr>
                    <Table.Td colSpan={8}>
                      <Center py="lg">
                        <Text c="dimmed">Нет активных групп ни у одного оператора</Text>
                      </Center>
                    </Table.Td>
                  </Table.Tr>
                ) : (
                  operators.map(op => <OperatorRow key={op.accountId} op={op} />)
                )}
              </Table.Tbody>
            </Table>
          </ScrollArea>
        </Paper>
      )}
    </Stack>
  );
}
