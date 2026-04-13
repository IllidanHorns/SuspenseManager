import { useState } from 'react';
import {
  Stack,
  Title,
  Text,
  SegmentedControl,
  MultiSelect,
  Button,
  Group,
  Paper,
  Table,
  ScrollArea,
  Badge,
  Alert,
  Loader,
  Center,
  TextInput,
  Box,
  Pagination,
  ActionIcon,
  Tooltip,
  Modal,
} from '@mantine/core';
import {
  IconSearch,
  IconCircleCheck,
  IconAlertCircle,
  IconRefresh,
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { useDisclosure } from '@mantine/hooks';
import { getGroupingPreview, commitGroup } from '../api/grouping';
import { useAuth } from '../hooks/useAuth';
import type { GroupingPreviewItem } from '../types';

const STATUS_0_COLS = [
  { value: 'Isrc', label: 'ISRC' },
  { value: 'Barcode', label: 'Штрих-код' },
  { value: 'CatalogNumber', label: 'Кат. номер' },
  { value: 'Artist', label: 'Исполнитель' },
  { value: 'TrackTitle', label: 'Название трека' },
  { value: 'Genre', label: 'Жанр' },
  { value: 'SenderCompany', label: 'Отправитель' },
  { value: 'RecipientCompany', label: 'Получатель' },
  { value: 'Operator', label: 'Оператор' },
  { value: 'AgreementType', label: 'Тип договора' },
  { value: 'AgreementNumber', label: 'Номер договора' },
  { value: 'TerritoryCode', label: 'Территория' },
];

const STATUS_1_COLS = [
  { value: 'ProductId', label: 'ID продукта' },
  { value: 'Isrc', label: 'ISRC' },
  { value: 'Barcode', label: 'Штрих-код' },
  { value: 'CatalogNumber', label: 'Кат. номер' },
  { value: 'ProductName', label: 'Название продукта' },
  { value: 'Artist', label: 'Исполнитель' },
  { value: 'SenderCompany', label: 'Отправитель' },
  { value: 'RecipientCompany', label: 'Получатель' },
  { value: 'Operator', label: 'Оператор' },
  { value: 'AgreementType', label: 'Тип договора' },
  { value: 'AgreementNumber', label: 'Номер договора' },
  { value: 'TerritoryCode', label: 'Территория' },
];

export function GroupingPage() {
  const { accountId } = useAuth();
  const [status, setStatus] = useState<'0' | '1'>('0');
  const [columns, setColumns] = useState<string[]>(['Artist', 'TrackTitle']);
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState<Record<string, string>>({});

  const [items, setItems] = useState<GroupingPreviewItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const [commitTarget, setCommitTarget] = useState<GroupingPreviewItem | null>(null);
  const [commitLoading, setCommitLoading] = useState(false);
  const [confirmOpened, { open: openConfirm, close: closeConfirm }] = useDisclosure(false);

  const availableCols = status === '0' ? STATUS_0_COLS : STATUS_1_COLS;

  const handlePreview = async (p = page) => {
    if (columns.length === 0) {
      setError('Выберите хотя бы одну колонку для группировки');
      return;
    }
    setError('');
    setLoading(true);
    try {
      const res = await getGroupingPreview({
        businessStatus: Number(status),
        groupByColumns: columns,
        pageNumber: p,
        pageSize: 20,
        ...filters,
      });
      setItems(res.items);
      setTotalCount(res.totalCount);
      setTotalPages(res.totalPages);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Ошибка предпросмотра');
    } finally {
      setLoading(false);
    }
  };

  const handlePageChange = (p: number) => {
    setPage(p);
    handlePreview(p);
  };

  const openCommitDialog = (item: GroupingPreviewItem) => {
    setCommitTarget(item);
    openConfirm();
  };

  const handleCommit = async () => {
    if (!commitTarget) return;
    setCommitLoading(true);
    try {
      await commitGroup({
        businessStatus: Number(status),
        groupByColumns: columns,
        keyValues: commitTarget.key,
        accountId,
      });
      notifications.show({
        title: 'Группа создана',
        message: `Зафиксирована группа из ${commitTarget.count} строк`,
        color: 'green',
        icon: <IconCircleCheck size={16} />,
      });
      closeConfirm();
      await handlePreview(page);
    } catch (e: unknown) {
      notifications.show({
        title: 'Ошибка',
        message: e instanceof Error ? e.message : 'Не удалось зафиксировать группу',
        color: 'red',
      });
    } finally {
      setCommitLoading(false);
    }
  };

  const handleStatusChange = (val: string) => {
    setStatus(val as '0' | '1');
    setColumns(val === '0' ? ['Artist', 'TrackTitle'] : ['ProductId']);
    setItems([]);
    setFilters({});
  };

  return (
    <Stack gap="xl">
      <Box>
        <Title order={3} fw={600}>Группировка суспенсов</Title>
        <Text c="dimmed" size="sm">Предпросмотр и фиксация динамических групп</Text>
      </Box>

      <Paper withBorder radius="md" p="md">
        <Stack gap="md">
          <Group align="flex-end" wrap="wrap" gap="md">
            <Box>
              <Text size="sm" fw={500} mb={4}>Тип суспенсов</Text>
              <SegmentedControl
                value={status}
                onChange={handleStatusChange}
                data={[
                  { label: 'Нет продукта (0)', value: '0' },
                  { label: 'Нет прав (1)', value: '1' },
                ]}
              />
            </Box>

            <Box style={{ flex: 1, minWidth: 280 }}>
              <MultiSelect
                label="Колонки для группировки"
                placeholder="Выберите колонки"
                data={availableCols}
                value={columns}
                onChange={setColumns}
                clearable
                searchable
              />
            </Box>

            <Button
              leftSection={<IconSearch size={16} />}
              color="indigo"
              onClick={() => { setPage(1); handlePreview(1); }}
              loading={loading}
            >
              Предпросмотр
            </Button>

            <Tooltip label="Обновить">
              <ActionIcon
                variant="light"
                color="gray"
                size="lg"
                onClick={() => handlePreview(page)}
                loading={loading}
              >
                <IconRefresh size={16} />
              </ActionIcon>
            </Tooltip>
          </Group>

          {columns.length > 0 && (
            <Box>
              <Text size="sm" fw={500} mb="xs" c="dimmed">Фильтры</Text>
              <Group gap="sm" wrap="wrap">
                {columns.map((col) => (
                  <TextInput
                    key={col}
                    size="xs"
                    placeholder={availableCols.find((c) => c.value === col)?.label ?? col}
                    value={filters[col] ?? ''}
                    onChange={(e) => setFilters((f) => ({ ...f, [col]: e.target.value }))}
                    style={{ width: 160 }}
                  />
                ))}
              </Group>
            </Box>
          )}
        </Stack>
      </Paper>

      {error && (
        <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">
          {error}
        </Alert>
      )}

      {loading ? (
        <Center py="xl"><Loader color="indigo" /></Center>
      ) : items.length > 0 ? (
        <Paper withBorder radius="md">
          <Group justify="space-between" p="md" style={{ borderBottom: '1px solid var(--mantine-color-default-border)' }}>
            <Text fw={600}>Результат группировки</Text>
            <Badge color="indigo" variant="light">{totalCount} групп</Badge>
          </Group>
          <ScrollArea>
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  {columns.map((col) => (
                    <Table.Th key={col}>
                      {availableCols.find((c) => c.value === col)?.label ?? col}
                    </Table.Th>
                  ))}
                  <Table.Th style={{ textAlign: 'right' }}>Кол-во</Table.Th>
                  <Table.Th style={{ textAlign: 'center' }}>Действие</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {items.map((item, idx) => (
                  <Table.Tr key={idx}>
                    {columns.map((col) => (
                      <Table.Td key={col}>
                        <Text size="sm">{item.key[col] ?? '—'}</Text>
                      </Table.Td>
                    ))}
                    <Table.Td style={{ textAlign: 'right' }}>
                      <Badge color="blue" variant="light">{item.count}</Badge>
                    </Table.Td>
                    <Table.Td style={{ textAlign: 'center' }}>
                      <Button
                        size="xs"
                        color="indigo"
                        variant="light"
                        onClick={() => openCommitDialog(item)}
                      >
                        Зафиксировать
                      </Button>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
          <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {totalCount.toLocaleString('ru-RU')}</Text>
            <Pagination value={page} onChange={handlePageChange} total={Math.max(1, totalPages)} size="sm" />
          </Group>
        </Paper>
      ) : null}

      {/* Confirm commit modal */}
      <Modal
        opened={confirmOpened}
        onClose={closeConfirm}
        title="Зафиксировать группу"
        centered
        radius="md"
      >
        <Stack gap="md">
          <Text size="sm">
            Создать группу из <strong>{commitTarget?.count}</strong> строк со следующими ключами:
          </Text>
          <Paper withBorder p="sm" radius="md">
            <Stack gap={4}>
              {commitTarget && Object.entries(commitTarget.key).map(([k, v]) => (
                <Group key={k} justify="space-between">
                  <Text size="sm" c="dimmed">{availableCols.find((c) => c.value === k)?.label ?? k}:</Text>
                  <Text size="sm" fw={500}>{v || '—'}</Text>
                </Group>
              ))}
            </Stack>
          </Paper>
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={closeConfirm}>Отмена</Button>
            <Button color="indigo" loading={commitLoading} onClick={handleCommit}>
              Создать группу
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}
