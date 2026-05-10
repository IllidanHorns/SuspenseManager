import { useState } from 'react';
import {
  Stack,
  Title,
  Text,
  Tabs,
  Paper,
  Table,
  ScrollArea,
  Group,
  Button,
  Badge,
  Pagination,
  Loader,
  Center,
  Alert,
  Box,
  ActionIcon,
  Tooltip,
  Switch,
  Select,
  TextInput,
  Modal,
  Timeline,
} from '@mantine/core';
import {
  IconHistory,
  IconAlertCircle,
  IconRefresh,
  IconEye,
  IconArrowRight,
  IconX,
  IconSearch,
  IconFolderOpen,
  IconList,
  IconUserCircle,
} from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import {
  getAuditGroups,
  getAuditGroupLogs,
  getAuditLines,
  getAuditLineLogs,
} from '../api/audit';
import { fmtDateTime } from '../utils/format';
import { StatusBadge } from '../components/common/StatusBadge';
import { PageSizeSelect } from '../components/common/PageSizeSelect';
import { ResizableTh } from '../components/common/ResizableTh';
import { CollapsibleFilters } from '../components/common/CollapsibleFilters';
import { STATUS_LABELS } from '../types';
import type { AuditGroupDto, AuditLineDto, AuditLogEntryDto, BusinessStatus } from '../types';
import { UserProfileModal } from '../components/common/UserProfileModal';
import { useDefaultPageSize } from '../hooks/useDefaultPageSize';

// Status transition arrow

function StatusArrow({ from, fromName, to, toName }: {
  from: number | null;
  fromName: string | null;
  to: number;
  toName: string | null;
}) {
  const fromLabel = from === null ? 'Создание' : (fromName ?? STATUS_LABELS[from] ?? String(from));
  const toLabel = toName ?? STATUS_LABELS[to] ?? String(to);
  return (
    <Group gap={6} wrap="nowrap">
      <Badge size="xs" variant="light" color="gray">{fromLabel}</Badge>
      <IconArrowRight size={12} color="var(--mantine-color-dimmed)" />
      <StatusBadge status={to as BusinessStatus} />
      <Text size="xs" c="dimmed" style={{ whiteSpace: 'nowrap' }}>{toLabel}</Text>
    </Group>
  );
}

// Log history modal

function LogHistoryModal({
  opened,
  onClose,
  title,
  logs,
  loading,
}: {
  opened: boolean;
  onClose: () => void;
  title: string;
  logs: AuditLogEntryDto[];
  loading: boolean;
}) {
  return (
    <Modal opened={opened} onClose={onClose} title={title} size="lg" centered>
      {loading ? (
        <Center py="xl"><Loader color="indigo" /></Center>
      ) : !logs.length ? (
        <Center py="xl">
          <Text c="dimmed">История пуста</Text>
        </Center>
      ) : (
        <Timeline active={-1} bulletSize={20} lineWidth={2} mt="sm">
          {logs.map((entry) => (
            <Timeline.Item
              key={entry.id}
              bullet={<IconHistory size={12} />}
              title={
                <StatusArrow
                  from={entry.statusFrom}
                  fromName={entry.statusFromName}
                  to={entry.statusTo}
                  toName={entry.statusToName}
                />
              }
            >
              <Text size="xs" c="dimmed" mt={2}>
                {entry.accountName
                  ? `${entry.accountName} (${entry.accountLogin})`
                  : entry.accountLogin}
                {' · '}
                {fmtDateTime(entry.operationTime)}
              </Text>
            </Timeline.Item>
          ))}
        </Timeline>
      )}
    </Modal>
  );
}

// Groups tab

const STATUS_OPTIONS = [
  { value: '', label: 'Все статусы' },
  { value: '0', label: 'Нет продукта (0)' },
  { value: '1', label: 'Нет прав (1)' },
  { value: '15', label: 'Группа — нет продукта (15)' },
  { value: '16', label: 'Группа — нет прав (16)' },
  { value: '30', label: 'Отложено — нет продукта (30)' },
  { value: '32', label: 'Отложено — нет прав (32)' },
  { value: '88', label: 'Валидировано (88)' },
  { value: '120', label: 'Бэк-офис — нет продукта (120)' },
  { value: '320', label: 'Бэк-офис — нет прав (320)' },
];

function GroupsTab() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useDefaultPageSize();
  const handlePageSizeChange = (v: number) => { setPageSize(v); setPage(1); };
  const [onlyMine, setOnlyMine] = useState(false);
  const [pendingStatus, setPendingStatus] = useState('');
  const [pendingFrom, setPendingFrom] = useState('');
  const [pendingTo, setPendingTo] = useState('');
  const [appliedStatus, setAppliedStatus] = useState<number | null>(null);
  const [appliedFrom, setAppliedFrom] = useState<string | null>(null);
  const [appliedTo, setAppliedTo] = useState<string | null>(null);

  const [selectedGroup, setSelectedGroup] = useState<AuditGroupDto | null>(null);
  const [selectedAccountId, setSelectedAccountId] = useState<number | null>(null);
  const [logsPage, setLogsPage] = useState(1);

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['audit-groups', page, pageSize, onlyMine, appliedStatus, appliedFrom, appliedTo],
    queryFn: () =>
      getAuditGroups({
        pageNumber: page,
        pageSize,
        onlyMine,
        status: appliedStatus ?? undefined,
        lastChangedFrom: appliedFrom ?? undefined,
        lastChangedTo: appliedTo ?? undefined,
      }),
  });

  const { data: logsData, isLoading: logsLoading } = useQuery({
    queryKey: ['audit-group-logs', selectedGroup?.id, logsPage],
    queryFn: () => getAuditGroupLogs(selectedGroup!.id, { pageNumber: logsPage, pageSize: 50 }),
    enabled: !!selectedGroup,
  });

  const applyFilters = () => {
    setAppliedStatus(pendingStatus ? Number(pendingStatus) : null);
    setAppliedFrom(pendingFrom || null);
    setAppliedTo(pendingTo || null);
    setPage(1);
  };

  const resetFilters = () => {
    setPendingStatus('');
    setPendingFrom('');
    setPendingTo('');
    setAppliedStatus(null);
    setAppliedFrom(null);
    setAppliedTo(null);
    setPage(1);
  };

  const hasActive = appliedStatus !== null || !!appliedFrom || !!appliedTo;
  const groupsFilterActiveCount =
    (appliedStatus !== null ? 1 : 0) + (appliedFrom ? 1 : 0) + (appliedTo ? 1 : 0);

  return (
    <Stack gap="md">
      {/* Toolbar */}
      <Group justify="space-between">
        <Switch
          label="Только мои"
          checked={onlyMine}
          onChange={(e) => { setOnlyMine(e.currentTarget.checked); setPage(1); }}
          size="sm"
        />
        <Tooltip label="Обновить">
          <ActionIcon variant="light" color="gray" onClick={() => refetch()}>
            <IconRefresh size={16} />
          </ActionIcon>
        </Tooltip>
      </Group>

      <CollapsibleFilters activeCount={groupsFilterActiveCount}>
        <Paper withBorder radius="md" p="sm">
          <Group gap="sm" align="flex-end" wrap="wrap">
            <Select
              size="xs"
              label="Статус"
              data={STATUS_OPTIONS}
              value={pendingStatus}
              onChange={(v) => setPendingStatus(v ?? '')}
              style={{ width: 220 }}
              clearable
            />
            <TextInput
              size="xs"
              label="Изменено от"
              type="date"
              value={pendingFrom}
              onChange={(e) => setPendingFrom(e.target.value)}
              style={{ width: 160 }}
            />
            <TextInput
              size="xs"
              label="Изменено до"
              type="date"
              value={pendingTo}
              onChange={(e) => setPendingTo(e.target.value)}
              style={{ width: 160 }}
            />
            <Group gap="xs">
              <Button size="xs" leftSection={<IconSearch size={12} />} onClick={applyFilters}>
                Найти
              </Button>
              {hasActive && (
                <Button size="xs" variant="subtle" color="gray" leftSection={<IconX size={12} />} onClick={resetFilters}>
                  Сбросить
                </Button>
              )}
            </Group>
          </Group>
        </Paper>
      </CollapsibleFilters>

      {/* Table */}
      <Paper withBorder radius="md">
        {isLoading ? (
          <Center py="xl"><Loader color="indigo" /></Center>
        ) : error ? (
          <Alert icon={<IconAlertCircle size={16} />} color="red" m="md">{(error as Error).message}</Alert>
        ) : !data?.items.length ? (
          <Center py="xl">
            <Stack align="center" gap="xs">
              <IconFolderOpen size={40} color="var(--mantine-color-dimmed)" />
              <Text c="dimmed">Нет групп</Text>
            </Stack>
          </Center>
        ) : (
          <ScrollArea>
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <ResizableTh>ID</ResizableTh>
                  <ResizableTh>Статус</ResizableTh>
                  <ResizableTh>Продукт</ResizableTh>
                  <ResizableTh>Создана</ResizableTh>
                  <ResizableTh>Создатель</ResizableTh>
                  <ResizableTh>Посл. изменение</ResizableTh>
                  <ResizableTh>Переход</ResizableTh>
                  <ResizableTh style={{ textAlign: 'center' }}>Лог</ResizableTh>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {data.items.map((g) => (
                  <Table.Tr key={g.id}>
                    <Table.Td>
                      <Text size="sm" fw={600} c="indigo">#{g.id}</Text>
                    </Table.Td>
                    <Table.Td>
                      <StatusBadge status={g.businessStatus as BusinessStatus} />
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">{g.productName ?? '—'}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="xs" c="dimmed">{fmtDateTime(g.createTime)}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Group gap={6}>
                        <ActionIcon
                          variant="subtle"
                          color="gray"
                          size="sm"
                          onClick={() => setSelectedAccountId(g.createdByAccountId)}
                          aria-label="Открыть профиль пользователя"
                        >
                          <IconUserCircle size={14} />
                        </ActionIcon>
                        <Text
                          size="xs"
                          style={{ cursor: 'pointer' }}
                          onClick={() => setSelectedAccountId(g.createdByAccountId)}
                        >
                          {g.createdByName ?? g.createdByLogin}
                        </Text>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Text size="xs" c="dimmed">{fmtDateTime(g.lastChangeTime)}</Text>
                    </Table.Td>
                    <Table.Td>
                      {g.lastStatusTo !== null && g.lastStatusTo !== undefined ? (
                        <StatusArrow
                          from={g.lastStatusFrom}
                          fromName={g.lastStatusFromName}
                          to={g.lastStatusTo}
                          toName={g.lastStatusToName}
                        />
                      ) : (
                        <Text size="xs" c="dimmed">—</Text>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Group justify="center">
                        <Tooltip label="История группы">
                          <ActionIcon
                            variant="light"
                            color="indigo"
                            size="sm"
                            onClick={() => { setSelectedGroup(g); setLogsPage(1); }}
                          >
                            <IconEye size={14} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
        )}
        {!isLoading && !error && (data?.items.length ?? 0) > 0 && (
          <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {(data?.totalCount ?? 0).toLocaleString('ru-RU')}</Text>
            <Group gap="sm">
              <PageSizeSelect value={pageSize} onChange={handlePageSizeChange} />
              <Pagination value={page} onChange={setPage} total={Math.max(1, data?.totalPages ?? 1)} size="sm" />
            </Group>
          </Group>
        )}
      </Paper>

      {/* Log modal */}
      <LogHistoryModal
        opened={!!selectedGroup}
        onClose={() => setSelectedGroup(null)}
        title={`История группы #${selectedGroup?.id}`}
        logs={logsData?.items ?? []}
        loading={logsLoading}
      />
      <UserProfileModal
        opened={!!selectedAccountId}
        onClose={() => setSelectedAccountId(null)}
        accountId={selectedAccountId}
      />
    </Stack>
  );
}

// Lines tab

function LinesTab() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useDefaultPageSize();
  const handlePageSizeChange = (v: number) => { setPageSize(v); setPage(1); };
  const [onlyMine, setOnlyMine] = useState(false);

  const [pendingStatus, setPendingStatus] = useState('');
  const [pendingGroupId, setPendingGroupId] = useState('');
  const [pendingFrom, setPendingFrom] = useState('');
  const [pendingTo, setPendingTo] = useState('');
  const [appliedStatus, setAppliedStatus] = useState<number | null>(null);
  const [appliedGroupId, setAppliedGroupId] = useState<number | null>(null);
  const [appliedFrom, setAppliedFrom] = useState<string | null>(null);
  const [appliedTo, setAppliedTo] = useState<string | null>(null);

  const [selectedLine, setSelectedLine] = useState<AuditLineDto | null>(null);
  const [logsPage, setLogsPage] = useState(1);

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['audit-lines', page, pageSize, onlyMine, appliedStatus, appliedGroupId, appliedFrom, appliedTo],
    queryFn: () =>
      getAuditLines({
        pageNumber: page,
        pageSize,
        onlyMine,
        status: appliedStatus ?? undefined,
        groupId: appliedGroupId ?? undefined,
        lastChangedFrom: appliedFrom ?? undefined,
        lastChangedTo: appliedTo ?? undefined,
      }),
  });

  const { data: logsData, isLoading: logsLoading } = useQuery({
    queryKey: ['audit-line-logs', selectedLine?.id, logsPage],
    queryFn: () => getAuditLineLogs(selectedLine!.id, { pageNumber: logsPage, pageSize: 50 }),
    enabled: !!selectedLine,
  });

  const applyFilters = () => {
    setAppliedStatus(pendingStatus ? Number(pendingStatus) : null);
    setAppliedGroupId(pendingGroupId ? Number(pendingGroupId) : null);
    setAppliedFrom(pendingFrom || null);
    setAppliedTo(pendingTo || null);
    setPage(1);
  };

  const resetFilters = () => {
    setPendingStatus('');
    setPendingGroupId('');
    setPendingFrom('');
    setPendingTo('');
    setAppliedStatus(null);
    setAppliedGroupId(null);
    setAppliedFrom(null);
    setAppliedTo(null);
    setPage(1);
  };

  const hasActive = appliedStatus !== null || appliedGroupId !== null || !!appliedFrom || !!appliedTo;
  const linesFilterActiveCount =
    (appliedStatus !== null ? 1 : 0) + (appliedGroupId !== null ? 1 : 0) + (appliedFrom ? 1 : 0) + (appliedTo ? 1 : 0);

  return (
    <Stack gap="md">
      {/* Toolbar */}
      <Group justify="space-between">
        <Switch
          label="Только мои"
          checked={onlyMine}
          onChange={(e) => { setOnlyMine(e.currentTarget.checked); setPage(1); }}
          size="sm"
        />
        <Tooltip label="Обновить">
          <ActionIcon variant="light" color="gray" onClick={() => refetch()}>
            <IconRefresh size={16} />
          </ActionIcon>
        </Tooltip>
      </Group>

      <CollapsibleFilters activeCount={linesFilterActiveCount}>
        <Paper withBorder radius="md" p="sm">
          <Group gap="sm" align="flex-end" wrap="wrap">
            <Select
              size="xs"
              label="Статус"
              data={STATUS_OPTIONS}
              value={pendingStatus}
              onChange={(v) => setPendingStatus(v ?? '')}
              style={{ width: 220 }}
              clearable
            />
            <TextInput
              size="xs"
              label="ID группы"
              placeholder="Например: 42"
              value={pendingGroupId}
              onChange={(e) => setPendingGroupId(e.target.value)}
              style={{ width: 130 }}
            />
            <TextInput
              size="xs"
              label="Изменено от"
              type="date"
              value={pendingFrom}
              onChange={(e) => setPendingFrom(e.target.value)}
              style={{ width: 160 }}
            />
            <TextInput
              size="xs"
              label="Изменено до"
              type="date"
              value={pendingTo}
              onChange={(e) => setPendingTo(e.target.value)}
              style={{ width: 160 }}
            />
            <Group gap="xs">
              <Button size="xs" leftSection={<IconSearch size={12} />} onClick={applyFilters}>
                Найти
              </Button>
              {hasActive && (
                <Button size="xs" variant="subtle" color="gray" leftSection={<IconX size={12} />} onClick={resetFilters}>
                  Сбросить
                </Button>
              )}
            </Group>
          </Group>
        </Paper>
      </CollapsibleFilters>

      {/* Table */}
      <Paper withBorder radius="md">
        {isLoading ? (
          <Center py="xl"><Loader color="indigo" /></Center>
        ) : error ? (
          <Alert icon={<IconAlertCircle size={16} />} color="red" m="md">{(error as Error).message}</Alert>
        ) : !data?.items.length ? (
          <Center py="xl">
            <Stack align="center" gap="xs">
              <IconList size={40} color="var(--mantine-color-dimmed)" />
              <Text c="dimmed">Нет строк</Text>
            </Stack>
          </Center>
        ) : (
          <ScrollArea>
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <ResizableTh>ID</ResizableTh>
                  <ResizableTh>Статус</ResizableTh>
                  <ResizableTh>Группа</ResizableTh>
                  <ResizableTh>ISRC</ResizableTh>
                  <ResizableTh>Исполнитель</ResizableTh>
                  <ResizableTh>Трек</ResizableTh>
                  <ResizableTh>Оператор</ResizableTh>
                  <ResizableTh>Посл. изменение</ResizableTh>
                  <ResizableTh>Переход</ResizableTh>
                  <ResizableTh style={{ textAlign: 'center' }}>Лог</ResizableTh>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {data.items.map((line) => (
                  <Table.Tr key={line.id}>
                    <Table.Td>
                      <Text size="sm" fw={600} c="indigo">#{line.id}</Text>
                    </Table.Td>
                    <Table.Td>
                      <StatusBadge status={line.businessStatus as BusinessStatus} />
                    </Table.Td>
                    <Table.Td>
                      {line.groupId ? (
                        <Badge variant="light" color="blue" size="sm">#{line.groupId}</Badge>
                      ) : (
                        <Text size="xs" c="dimmed">—</Text>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Text size="xs" ff="monospace">{line.isrc ?? '—'}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{line.artist ?? '—'}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{line.trackTitle ?? '—'}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="xs" c="dimmed">{line.operator ?? '—'}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="xs" c="dimmed">{fmtDateTime(line.lastChangeTime)}</Text>
                    </Table.Td>
                    <Table.Td>
                      {line.lastStatusTo !== null && line.lastStatusTo !== undefined ? (
                        <StatusArrow
                          from={line.lastStatusFrom}
                          fromName={line.lastStatusFromName}
                          to={line.lastStatusTo}
                          toName={line.lastStatusToName}
                        />
                      ) : (
                        <Text size="xs" c="dimmed">—</Text>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Group justify="center">
                        <Tooltip label="История строки">
                          <ActionIcon
                            variant="light"
                            color="indigo"
                            size="sm"
                            onClick={() => { setSelectedLine(line); setLogsPage(1); }}
                          >
                            <IconEye size={14} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
        )}
        {!isLoading && !error && (data?.items.length ?? 0) > 0 && (
          <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {(data?.totalCount ?? 0).toLocaleString('ru-RU')}</Text>
            <Group gap="sm">
              <PageSizeSelect value={pageSize} onChange={handlePageSizeChange} />
              <Pagination value={page} onChange={setPage} total={Math.max(1, data?.totalPages ?? 1)} size="sm" />
            </Group>
          </Group>
        )}
      </Paper>

      {/* Log modal */}
      <LogHistoryModal
        opened={!!selectedLine}
        onClose={() => setSelectedLine(null)}
        title={`История строки #${selectedLine?.id}`}
        logs={logsData?.items ?? []}
        loading={logsLoading}
      />
    </Stack>
  );
}

// Page

export function AuditPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = searchParams.get('tab') ?? 'groups';

  return (
    <Stack gap="xl">
      <Box>
        <Title order={3} fw={600}>Аудит</Title>
        <Text c="dimmed" size="sm">История изменений статусов групп и суспенс-строк</Text>
      </Box>

      <Tabs
        value={tab}
        onChange={(v) => setSearchParams({ tab: v ?? 'groups' })}
        variant="outline"
        radius="md"
      >
        <Tabs.List>
          <Tabs.Tab value="groups" leftSection={<IconFolderOpen size={14} />}>
            Группы
          </Tabs.Tab>
          <Tabs.Tab value="lines" leftSection={<IconList size={14} />}>
            Строки суспенсов
          </Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="groups" pt="md">
          <GroupsTab />
        </Tabs.Panel>
        <Tabs.Panel value="lines" pt="md">
          <LinesTab />
        </Tabs.Panel>
      </Tabs>
    </Stack>
  );
}
