import { useState } from 'react';
import {
  Stack,
  Title,
  Text,
  Paper,
  Table,
  ScrollArea,
  Group,
  Badge,
  Pagination,
  Loader,
  Center,
  Alert,
  Box,
  SegmentedControl,
  ActionIcon,
  Tooltip,
  TextInput,
  Button,
} from '@mantine/core';
import { IconAlertCircle, IconRefresh, IconList, IconSearch, IconX } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { getSuspenses, getUngroupedSuspenses } from '../api/suspenses';
import { useSearchParams } from 'react-router-dom';
import { StatusBadge } from '../components/common/StatusBadge';

interface FilterValues {
  isrc: string;
  artist: string;
  operator: string;
  territory: string;
}

const EMPTY_FILTERS: FilterValues = { isrc: '', artist: '', operator: '', territory: '' };

function buildFilters(v: FilterValues): Record<string, string> {
  const f: Record<string, string> = {};
  if (v.isrc.trim())      f['Isrc_contains']          = v.isrc.trim();
  if (v.artist.trim())    f['Artist_contains']         = v.artist.trim();
  if (v.operator.trim())  f['Operator_contains']       = v.operator.trim();
  if (v.territory.trim()) f['TerritoryCode_contains']  = v.territory.trim();
  return f;
}

export function SuspensesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const mode = searchParams.get('status') ?? 'all';
  const [page, setPage] = useState(1);

  const [pending, setPending] = useState<FilterValues>(EMPTY_FILTERS);
  const [applied, setApplied] = useState<Record<string, string>>({});

  const hasActive = Object.keys(applied).length > 0;

  const applyFilters = () => {
    setApplied(buildFilters(pending));
    setPage(1);
  };

  const resetFilters = () => {
    setPending(EMPTY_FILTERS);
    setApplied({});
    setPage(1);
  };

  const onKey = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') applyFilters();
  };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['suspenses', mode, page, applied],
    queryFn: () => {
      const params = { pageNumber: page, pageSize: 30, Filters: applied };
      if (mode === '0') return getUngroupedSuspenses(0, params);
      if (mode === '1') return getUngroupedSuspenses(1, params);
      return getSuspenses(params);
    },
  });

  const rows = data?.items ?? [];

  return (
    <Stack gap="xl">
      <Box>
        <Title order={3} fw={600}>Суспенс-строки</Title>
        <Text c="dimmed" size="sm">Отдельные строки суспенсов из стриминговых отчётов</Text>
      </Box>

      <Group justify="space-between" wrap="wrap" gap="md">
        <SegmentedControl
          value={mode}
          onChange={(v) => { setPage(1); setApplied({}); setPending(EMPTY_FILTERS); setSearchParams({ status: v }); }}
          data={[
            { label: 'Все', value: 'all' },
            { label: 'Нет продукта (0)', value: '0' },
            { label: 'Нет прав (1)', value: '1' },
          ]}
        />
        <Group gap="xs">
          {data && (
            <Badge variant="light" color="indigo">
              Всего: {data.totalCount.toLocaleString('ru-RU')}
            </Badge>
          )}
          <Tooltip label="Обновить">
            <ActionIcon variant="light" color="gray" onClick={() => refetch()}>
              <IconRefresh size={16} />
            </ActionIcon>
          </Tooltip>
        </Group>
      </Group>

      {/* Filter bar */}
      <Paper withBorder radius="md" p="sm">
        <Group gap="sm" wrap="wrap" align="flex-end">
          <TextInput
            size="xs"
            placeholder="ISRC"
            label="ISRC"
            value={pending.isrc}
            onChange={(e) => setPending(p => ({ ...p, isrc: e.target.value }))}
            onKeyDown={onKey}
            style={{ flex: 1, minWidth: 120 }}
          />
          <TextInput
            size="xs"
            placeholder="Исполнитель"
            label="Исполнитель"
            value={pending.artist}
            onChange={(e) => setPending(p => ({ ...p, artist: e.target.value }))}
            onKeyDown={onKey}
            style={{ flex: 1, minWidth: 140 }}
          />
          <TextInput
            size="xs"
            placeholder="Оператор"
            label="Оператор"
            value={pending.operator}
            onChange={(e) => setPending(p => ({ ...p, operator: e.target.value }))}
            onKeyDown={onKey}
            style={{ flex: 1, minWidth: 120 }}
          />
          <TextInput
            size="xs"
            placeholder="Территория"
            label="Территория"
            value={pending.territory}
            onChange={(e) => setPending(p => ({ ...p, territory: e.target.value }))}
            onKeyDown={onKey}
            style={{ flex: '0 0 100px' }}
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

      {isLoading ? (
        <Center py="xl"><Loader color="indigo" /></Center>
      ) : error ? (
        <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">
          {error.message}
        </Alert>
      ) : !rows.length ? (
        <Center py="xl">
          <Stack align="center" gap="xs">
            <IconList size={40} color="var(--mantine-color-dimmed)" />
            <Text c="dimmed">{hasActive ? 'Ничего не найдено' : 'Нет строк'}</Text>
          </Stack>
        </Center>
      ) : (
        <Paper withBorder radius="md">
          <ScrollArea>
            <Table striped highlightOnHover style={{ minWidth: 1100 }}>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>ID</Table.Th>
                  <Table.Th>Статус</Table.Th>
                  <Table.Th>ISRC</Table.Th>
                  <Table.Th>Исполнитель</Table.Th>
                  <Table.Th>Трек</Table.Th>
                  <Table.Th>Оператор</Table.Th>
                  <Table.Th>Территория</Table.Th>
                  <Table.Th>Отправитель</Table.Th>
                  <Table.Th>Qty</Table.Th>
                  <Table.Th>PPD</Table.Th>
                  <Table.Th>Группа</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {rows.map((s) => (
                  <Table.Tr key={s.id}>
                    <Table.Td><Text size="sm" fw={600}>{s.id}</Text></Table.Td>
                    <Table.Td><StatusBadge status={s.businessStatus} /></Table.Td>
                    <Table.Td><Text size="sm" ff="monospace">{s.isrc ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.artist ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.trackTitle ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.operator ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.territoryCode ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.senderCompany ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.qty}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.ppd}</Text></Table.Td>
                    <Table.Td>
                      {s.groupId
                        ? <Badge variant="light" color="blue" size="sm">#{s.groupId}</Badge>
                        : <Text size="sm" c="dimmed">—</Text>}
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
          <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {data!.totalCount.toLocaleString('ru-RU')}</Text>
            <Pagination value={page} onChange={setPage} total={Math.max(1, data!.totalPages)} size="sm" />
          </Group>
        </Paper>
      )}
    </Stack>
  );
}
