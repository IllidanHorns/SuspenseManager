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
  TextInput,
} from '@mantine/core';
import {
  IconFolderOpen,
  IconAlertCircle,
  IconRefresh,
  IconEye,
  IconDownload,
  IconSearch,
  IconX,
} from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { getNoProductGroups, getNoRightsGroups } from '../api/groups';
import { exportGroupsByStatus } from '../api/processing';
import { downloadBlob, fmtDateTime } from '../utils/format';
import { StatusBadge } from '../components/common/StatusBadge';
import { notifications } from '@mantine/notifications';
import type { SuspenseGroup } from '../types';

function GroupTable({
  groups,
  loading,
  error,
  page,
  totalPages,
  totalCount,
  onPageChange,
}: {
  groups: SuspenseGroup[];
  loading: boolean;
  error: string;
  page: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (p: number) => void;
}) {
  const navigate = useNavigate();

  const body = loading ? (
    <Center py="xl"><Loader color="indigo" /></Center>
  ) : error ? (
    <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">{error}</Alert>
  ) : !groups.length ? (
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
            <Table.Th>ID</Table.Th>
            <Table.Th>Статус</Table.Th>
            <Table.Th>Строк</Table.Th>
            <Table.Th>Продукт</Table.Th>
            <Table.Th>Создана</Table.Th>
            <Table.Th style={{ textAlign: 'center' }}>Действия</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {groups.map((g) => (
            <Table.Tr key={g.id}>
              <Table.Td>
                <Text size="sm" fw={600} c="indigo">#{g.id}</Text>
              </Table.Td>
              <Table.Td><StatusBadge status={g.businessStatus} /></Table.Td>
              <Table.Td>
                <Badge variant="light" color="blue" size="sm">
                  {g.suspenseCount ?? '—'}
                </Badge>
              </Table.Td>
              <Table.Td>
                <Text size="sm" c="dimmed">
                  {g.catalogProduct?.productName ??
                    g.metadata?.title ??
                    (g.catalogProductId ? `ID: ${g.catalogProductId}` : '—')}
                </Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm" c="dimmed">{fmtDateTime(g.createTime)}</Text>
              </Table.Td>
              <Table.Td>
                <Group justify="center" gap="xs">
                  <Tooltip label="Открыть группу">
                    <ActionIcon
                      variant="light"
                      color="indigo"
                      size="sm"
                      onClick={() => navigate(`/groups/${g.id}`)}
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
  );

  return (
    <>
      {body}
      {!loading && !error && (
        <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
          <Text size="sm" c="dimmed">Всего: {totalCount.toLocaleString('ru-RU')}</Text>
          <Pagination value={page} onChange={onPageChange} total={Math.max(1, totalPages)} size="sm" />
        </Group>
      )}
    </>
  );
}

function TabContent({ type }: { type: 'no-product' | 'no-rights' }) {
  const [page, setPage] = useState(1);
  const status = type === 'no-product' ? 15 : 16;

  const [pendingId, setPendingId] = useState('');
  const [applied, setApplied] = useState<Record<string, string>>({});
  const hasActive = Object.keys(applied).length > 0;

  const applyFilters = () => {
    const f: Record<string, string> = {};
    if (pendingId.trim()) f['Id'] = pendingId.trim();
    setApplied(f);
    setPage(1);
  };

  const resetFilters = () => {
    setPendingId('');
    setApplied({});
    setPage(1);
  };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['groups', type, page, applied],
    queryFn: () => type === 'no-product'
      ? getNoProductGroups({ pageNumber: page, pageSize: 20, Filters: applied })
      : getNoRightsGroups({ pageNumber: page, pageSize: 20, Filters: applied }),
  });

  const handleExport = async () => {
    try {
      const blob = await exportGroupsByStatus(status as 15 | 16);
      downloadBlob(blob, `groups_status_${status}.xlsx`);
    } catch (e: unknown) {
      notifications.show({
        title: 'Ошибка',
        message: e instanceof Error ? e.message : 'Ошибка экспорта',
        color: 'red',
      });
    }
  };

  return (
    <Stack gap="md">
      <Group justify="space-between" gap="xs">
        <Tooltip label="Обновить">
          <ActionIcon variant="light" color="gray" onClick={() => refetch()}>
            <IconRefresh size={16} />
          </ActionIcon>
        </Tooltip>
        <Button
          size="xs"
          variant="light"
          color="green"
          leftSection={<IconDownload size={14} />}
          onClick={handleExport}
        >
          Экспорт
        </Button>
      </Group>

      {/* Filter bar */}
      <Paper withBorder radius="md" p="sm">
        <Group gap="sm" align="flex-end">
          <TextInput
            size="xs"
            label="ID группы"
            placeholder="Например: 42"
            value={pendingId}
            onChange={(e) => setPendingId(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && applyFilters()}
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

      <Paper withBorder radius="md">
        <GroupTable
          groups={data?.items ?? []}
          loading={isLoading}
          error={error?.message ?? ''}
          page={page}
          totalPages={data?.totalPages ?? 1}
          totalCount={data?.totalCount ?? 0}
          onPageChange={setPage}
        />
      </Paper>
    </Stack>
  );
}

export function SavedGroupsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = searchParams.get('tab') ?? '0';

  return (
    <Stack gap="xl">
      <Box>
        <Title order={3} fw={600}>Сохранённые группы</Title>
        <Text c="dimmed" size="sm">Группы суспенсов, готовые к обработке</Text>
      </Box>

      <Tabs
        value={tab}
        onChange={(v) => setSearchParams({ tab: v ?? '0' })}
        variant="outline"
        radius="md"
      >
        <Tabs.List>
          <Tabs.Tab value="0" leftSection={<IconFolderOpen size={14} />}>
            Нет продукта (статус 15)
          </Tabs.Tab>
          <Tabs.Tab value="1" leftSection={<IconFolderOpen size={14} />}>
            Нет прав (статус 16)
          </Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="0" pt="md">
          <TabContent type="no-product" />
        </Tabs.Panel>
        <Tabs.Panel value="1" pt="md">
          <TabContent type="no-rights" />
        </Tabs.Panel>
      </Tabs>
    </Stack>
  );
}
