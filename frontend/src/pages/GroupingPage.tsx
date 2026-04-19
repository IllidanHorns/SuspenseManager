import { useState, useMemo } from 'react';
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
  NumberInput,
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
  IconArrowUp,
  IconArrowDown,
  IconArrowsUpDown,
  IconDownload,
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { useDisclosure } from '@mantine/hooks';
import { getGroupingPreview, commitGroup, previewGroupLines, exportPreviewLines } from '../api/grouping';
import { exportGroupSuspenses } from '../api/processing';
import { downloadBlob } from '../utils/format';
import { useAuth } from '../hooks/useAuth';
import { PageSizeSelect } from '../components/common/PageSizeSelect';
import { ResizableTh } from '../components/common/ResizableTh';
import { CollapsibleFilters } from '../components/common/CollapsibleFilters';
import { useDefaultPageSize } from '../hooks/useDefaultPageSize';
import type { GroupingPreviewItem, SuspenseLinePreviewDto } from '../types';

const STATUS_0_COLS = [
  { value: 'Isrc', label: 'ISRC' },
  { value: 'Barcode', label: 'Баркод' },
  { value: 'CatalogNumber', label: 'Кат. номер' },
  { value: 'Artist', label: 'Исполнитель' },
  { value: 'TrackTitle', label: 'Название трека' },
  { value: 'Genre', label: 'Жанр' },
  { value: 'SenderCompany', label: 'Отправитель' },
  { value: 'RecipientCompany', label: 'Получатель' },
  { value: 'Operator', label: 'Оператор' },
  { value: 'TerritoryCode', label: 'Территория' },
];

// Используется только для отображения лейблов (не входит в список выбора)
const PRODUCT_ID_COL = { value: 'ProductId', label: 'Идентификатор продукта' };

const STATUS_1_COLS = [
  { value: 'Isrc', label: 'ISRC' },
  { value: 'Barcode', label: 'Баркод' },
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
  const [pageSize, setPageSize] = useDefaultPageSize();
  const [filters, setFilters] = useState<Record<string, string>>({});

  const [items, setItems] = useState<GroupingPreviewItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const [sortBy, setSortBy] = useState<string>('Count');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');
  const [countMin, setCountMin] = useState<number | string>('');
  const [countMax, setCountMax] = useState<number | string>('');
  const [revenueMin, setRevenueMin] = useState<number | string>('');
  const [revenueMax, setRevenueMax] = useState<number | string>('');

  const [linesTarget, setLinesTarget] = useState<GroupingPreviewItem | null>(null);
  const [linesData, setLinesData] = useState<SuspenseLinePreviewDto[]>([]);
  const [linesTotalCount, setLinesTotalCount] = useState(0);
  const [linesTotalPages, setLinesTotalPages] = useState(1);
  const [linesPage, setLinesPage] = useState(1);
  const [linesPageSize] = useState(20);
  const [linesLoading, setLinesLoading] = useState(false);
  const [linesOpened, { open: openLines, close: closeLines }] = useDisclosure(false);

  const [linesExporting, setLinesExporting] = useState(false);

  const [commitTarget, setCommitTarget] = useState<GroupingPreviewItem | null>(null);
  const [commitLoading, setCommitLoading] = useState(false);
  const [committedGroupId, setCommittedGroupId] = useState<number | null>(null);
  const [commitExporting, setCommitExporting] = useState(false);
  const [confirmOpened, { open: openConfirm, close: closeConfirm }] = useDisclosure(false);

  const availableCols = status === '0' ? STATUS_0_COLS : STATUS_1_COLS;
  const allColLabels = [PRODUCT_ID_COL, ...STATUS_0_COLS, ...STATUS_1_COLS];
  const colLabel = (col: string) => allColLabels.find((c) => c.value === col)?.label ?? col;

  const groupingFiltersActiveCount = useMemo(() => {
    let n = Object.values(filters).filter((v) => v && String(v).trim() !== '').length;
    if (countMin !== '' && countMin != null) n += 1;
    if (countMax !== '' && countMax != null) n += 1;
    if (revenueMin !== '' && revenueMin != null) n += 1;
    if (revenueMax !== '' && revenueMax != null) n += 1;
    return n;
  }, [filters, countMin, countMax, revenueMin, revenueMax]);

  const handleColumnsChange = (val: string[]) => {
    setColumns(val);
    if (val.length === 0) setItems([]);
  };

  const handlePreview = async (
    p = page,
    overrides?: { sortBy?: string; sortDir?: 'asc' | 'desc' }
  ) => {
    if (columns.length === 0) {
      setError('Выберите хотя бы одну колонку для группировки');
      return;
    }
    setError('');
    setLoading(true);
    const effectiveSortBy = overrides?.sortBy ?? sortBy;
    const effectiveSortDir = overrides?.sortDir ?? sortDir;
    try {
      const res = await getGroupingPreview({
        businessStatus: Number(status),
        groupByColumns: effectiveColumns,
        pageNumber: p,
        pageSize,
        sortBy: effectiveSortBy,
        sortDirection: effectiveSortDir,
        ...(countMin !== '' ? { countMin: Number(countMin) } : {}),
        ...(countMax !== '' ? { countMax: Number(countMax) } : {}),
        ...(revenueMin !== '' ? { revenueMin: Number(revenueMin) } : {}),
        ...(revenueMax !== '' ? { revenueMax: Number(revenueMax) } : {}),
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

  const handlePageSizeChange = (v: number) => {
    setPageSize(v);
    setPage(1);
    handlePreview(1);
  };

  const loadLines = async (item: GroupingPreviewItem, p: number) => {
    setLinesLoading(true);
    try {
      const res = await previewGroupLines({
        businessStatus: Number(status),
        groupByColumns: effectiveColumns,
        keyValues: item.key,
        pageNumber: p,
        pageSize: linesPageSize,
      });
      setLinesData(res.items);
      setLinesTotalCount(res.totalCount);
      setLinesTotalPages(res.totalPages);
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка загрузки строк', color: 'red' });
    } finally {
      setLinesLoading(false);
    }
  };

  const openLinesModal = (item: GroupingPreviewItem) => {
    setLinesTarget(item);
    setLinesPage(1);
    openLines();
    loadLines(item, 1);
  };

  const handleLinesPageChange = (p: number) => {
    setLinesPage(p);
    if (linesTarget) loadLines(linesTarget, p);
  };

  const handleExportLines = async () => {
    if (!linesTarget) return;
    setLinesExporting(true);
    try {
      const blob = await exportPreviewLines({
        businessStatus: Number(status),
        groupByColumns: effectiveColumns,
        keyValues: linesTarget.key,
      });
      downloadBlob(blob, 'suspenses_preview.xlsx');
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка экспорта', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setLinesExporting(false);
    }
  };

  const handleExportCommitted = async () => {
    if (!committedGroupId) return;
    setCommitExporting(true);
    try {
      const blob = await exportGroupSuspenses(committedGroupId);
      downloadBlob(blob, `group_${committedGroupId}_suspenses.xlsx`);
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка экспорта', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setCommitExporting(false);
    }
  };

  const openCommitDialog = (item: GroupingPreviewItem) => {
    setCommitTarget(item);
    setCommittedGroupId(null);
    openConfirm();
  };

  const closeCommitDialog = () => {
    closeConfirm();
    setCommittedGroupId(null);
    setCommitTarget(null);
  };

  const handleCommit = async (withExport = false) => {
    if (!commitTarget) return;
    setCommitLoading(true);
    try {
      const group = await commitGroup({
        businessStatus: Number(status),
        groupByColumns: effectiveColumns,
        keyValues: commitTarget.key,
        accountId,
      });
      const gid = (group as { id: number }).id;
      setCommittedGroupId(gid);
      notifications.show({
        title: 'Группа создана',
        message: `Зафиксирована группа #${gid} из ${commitTarget.count} строк`,
        color: 'green',
        icon: <IconCircleCheck size={16} />,
      });
      await handlePreview(page);
      if (withExport) {
        try {
          const blob = await exportGroupSuspenses(gid);
          downloadBlob(blob, `group_${gid}_suspenses.xlsx`);
        } catch {
          notifications.show({ title: 'Ошибка экспорта', message: 'Не удалось скачать файл', color: 'red' });
        }
      }
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

  // Для статуса 1 ProductId всегда добавляется автоматически — не отображается в UI
  const effectiveColumns = status === '1' ? ['ProductId', ...columns] : columns;

  const handleStatusChange = (val: string) => {
    setStatus(val as '0' | '1');
    setColumns(val === '0' ? ['Artist', 'TrackTitle'] : ['Artist']);
    setItems([]);
    setFilters({});
    setCountMin(''); setCountMax(''); setRevenueMin(''); setRevenueMax('');
  };

  const handleSort = (col: string) => {
    const newDir = sortBy === col ? (sortDir === 'asc' ? 'desc' : 'asc') : 'desc';
    setSortBy(col);
    setSortDir(newDir);
    handlePreview(page, { sortBy: col, sortDir: newDir });
  };

  const SortIcon = ({ col }: { col: string }) => {
    if (sortBy !== col) return <IconArrowsUpDown size={13} style={{ opacity: 0.4 }} />;
    return sortDir === 'asc' ? <IconArrowUp size={13} /> : <IconArrowDown size={13} />;
  };

  return (
    <Stack gap="xl">
      {/* Модал предпросмотра строк */}
      <Modal
        opened={linesOpened}
        onClose={closeLines}
        title="Строки суспенса в группе"
        size="90%"
      >
        {linesTarget && (
          <Stack gap="md">
            {/* Шапка: критерии + кнопка экспорта всегда видна */}
            <Group justify="space-between" align="flex-start">
              <Paper withBorder radius="md" p="sm" style={{ flex: 1 }}>
                <Text size="sm" fw={600} mb="xs" c="dimmed">Критерии группировки</Text>
                <Group gap="sm" wrap="wrap">
                  {effectiveColumns.map((col) => (
                    <Box key={col}>
                      <Text size="xs" c="dimmed">{colLabel(col)}</Text>
                      <Text size="sm" fw={500}>{linesTarget.key[col] ?? '—'}</Text>
                    </Box>
                  ))}
                </Group>
              </Paper>
              <Button
                variant="light"
                color="green"
                leftSection={<IconDownload size={14} />}
                onClick={handleExportLines}
                loading={linesExporting}
                mt={4}
              >
                Скачать Excel
              </Button>
            </Group>

            {linesLoading ? (
              <Center py="xl"><Loader color="indigo" /></Center>
            ) : (
              <>
                <ScrollArea h={480}>
                  <Table striped highlightOnHover style={{ minWidth: 1200 }}>
                    <Table.Thead>
                      <Table.Tr>
                        <ResizableTh>ID</ResizableTh>
                        <ResizableTh>ISRC</ResizableTh>
                        <ResizableTh>Баркод</ResizableTh>
                        <ResizableTh>Артист</ResizableTh>
                        <ResizableTh>Трек</ResizableTh>
                        <ResizableTh>Жанр</ResizableTh>
                        <ResizableTh>Оператор</ResizableTh>
                        <ResizableTh>Отправитель</ResizableTh>
                        <ResizableTh>Получатель</ResizableTh>
                        <ResizableTh>Территория</ResizableTh>
                        <ResizableTh>Тип дог.</ResizableTh>
                        <ResizableTh>№ дог.</ResizableTh>
                        <ResizableTh style={{ textAlign: 'right' }}>Кол-во</ResizableTh>
                        <ResizableTh style={{ textAlign: 'right' }}>Курс обмена</ResizableTh>
                      </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                      {linesData.map((line) => (
                        <Table.Tr key={line.id}>
                          <Table.Td><Text size="xs" c="dimmed">{line.id}</Text></Table.Td>
                          <Table.Td><Text size="xs">{line.isrc ?? '—'}</Text></Table.Td>
                          <Table.Td><Text size="xs">{line.barcode ?? '—'}</Text></Table.Td>
                          <Table.Td><Text size="xs">{line.artist ?? '—'}</Text></Table.Td>
                          <Table.Td><Text size="xs">{line.trackTitle ?? '—'}</Text></Table.Td>
                          <Table.Td><Text size="xs">{line.genre ?? '—'}</Text></Table.Td>
                          <Table.Td><Text size="xs">{line.operator ?? '—'}</Text></Table.Td>
                          <Table.Td><Text size="xs">{line.senderCompany ?? '—'}</Text></Table.Td>
                          <Table.Td><Text size="xs">{line.recipientCompany ?? '—'}</Text></Table.Td>
                          <Table.Td><Text size="xs">{line.territoryCode ?? '—'}</Text></Table.Td>
                          <Table.Td><Text size="xs">{line.agreementType ?? '—'}</Text></Table.Td>
                          <Table.Td><Text size="xs">{line.agreementNumber ?? '—'}</Text></Table.Td>
                          <Table.Td style={{ textAlign: 'right' }}><Text size="xs">{line.qty.toLocaleString('ru-RU')}</Text></Table.Td>
                          <Table.Td style={{ textAlign: 'right' }}><Text size="xs">{line.exchangeRate}</Text></Table.Td>
                        </Table.Tr>
                      ))}
                    </Table.Tbody>
                  </Table>
                </ScrollArea>
                <Group justify="space-between">
                  <Text size="sm" c="dimmed">Всего: {linesTotalCount.toLocaleString('ru-RU')} строк</Text>
                  {linesTotalPages > 1 && (
                    <Pagination total={linesTotalPages} value={linesPage} onChange={handleLinesPageChange} size="sm" />
                  )}
                </Group>
              </>
            )}
          </Stack>
        )}
      </Modal>

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
                description={status === '1' ? 'Идентификатор продукта всегда включён в группировку автоматически' : undefined}
                placeholder="Выберите колонки"
                data={availableCols}
                value={columns}
                onChange={handleColumnsChange}
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

          <CollapsibleFilters activeCount={groupingFiltersActiveCount}>
            <Stack gap="md">
              {effectiveColumns.length > 0 && (
                <Box>
                  <Text size="sm" fw={500} mb="xs" c="dimmed">Фильтры по колонкам</Text>
                  <Group gap="sm" wrap="wrap">
                    {effectiveColumns.map((col) => (
                      <TextInput
                        key={col}
                        size="xs"
                        placeholder={colLabel(col)}
                        value={filters[col] ?? ''}
                        onChange={(e) => setFilters((f) => ({ ...f, [col]: e.target.value }))}
                        onKeyDown={(e) => { if (e.key === 'Enter') { setPage(1); handlePreview(1); } }}
                        style={{ width: 160 }}
                      />
                    ))}
                  </Group>
                </Box>
              )}

              <Box>
                <Text size="sm" fw={500} mb="xs" c="dimmed">Диапазоны</Text>
                <Group gap="sm" wrap="wrap" align="flex-end">
                  <NumberInput
                    size="xs"
                    placeholder="Кол-во от"
                    value={countMin}
                    onChange={setCountMin}
                    min={0}
                    style={{ width: 130 }}
                  />
                  <NumberInput
                    size="xs"
                    placeholder="Кол-во до"
                    value={countMax}
                    onChange={setCountMax}
                    min={0}
                    style={{ width: 130 }}
                  />
                  <NumberInput
                    size="xs"
                    placeholder="Выручка от, ₽"
                    value={revenueMin}
                    onChange={setRevenueMin}
                    min={0}
                    decimalScale={2}
                    style={{ width: 150 }}
                  />
                  <NumberInput
                    size="xs"
                    placeholder="Выручка до, ₽"
                    value={revenueMax}
                    onChange={setRevenueMax}
                    min={0}
                    decimalScale={2}
                    style={{ width: 150 }}
                  />
                  <Button
                    size="xs"
                    variant="light"
                    color="indigo"
                    onClick={() => { setPage(1); handlePreview(1); }}
                  >
                    Применить
                  </Button>
                </Group>
              </Box>
            </Stack>
          </CollapsibleFilters>
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
                  {effectiveColumns.map((col) => (
                    <ResizableTh key={col}>
                      {colLabel(col)}
                    </ResizableTh>
                  ))}
                  <ResizableTh style={{ textAlign: 'right', cursor: 'pointer', userSelect: 'none' }} onClick={() => handleSort('Count')}>
                    <Group gap={4} justify="flex-end">Кол-во <SortIcon col="Count" /></Group>
                  </ResizableTh>
                  <ResizableTh style={{ textAlign: 'right', cursor: 'pointer', userSelect: 'none' }} onClick={() => handleSort('RevenueRub')}>
                    <Group gap={4} justify="flex-end">Выручка, ₽ <SortIcon col="RevenueRub" /></Group>
                  </ResizableTh>
                  <ResizableTh style={{ textAlign: 'center' }}>Действие</ResizableTh>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {items.map((item, idx) => (
                  <Table.Tr key={idx}>
                    {effectiveColumns.map((col) => (
                      <Table.Td key={col}>
                        <Text size="sm">{item.key[col] ?? '—'}</Text>
                      </Table.Td>
                    ))}
                    <Table.Td style={{ textAlign: 'right' }}>
                      <Badge color="blue" variant="light">{item.count}</Badge>
                    </Table.Td>
                    <Table.Td style={{ textAlign: 'right' }}>
                      <Text size="sm" fw={500}>
                        {item.revenueRub.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                      </Text>
                    </Table.Td>
                    <Table.Td style={{ textAlign: 'center' }}>
                      <Group gap="xs" justify="center">
                        <Button size="xs" variant="subtle" color="gray" onClick={() => openLinesModal(item)}>
                          Строки
                        </Button>
                        <Button size="xs" color="indigo" variant="light" onClick={() => openCommitDialog(item)}>
                          Зафиксировать
                        </Button>
                        <Tooltip label="Скачать строки в Excel">
                          <ActionIcon
                            size="sm"
                            variant="subtle"
                            color="green"
                            onClick={() =>
                              exportPreviewLines({
                                businessStatus: Number(status),
                                groupByColumns: effectiveColumns,
                                keyValues: item.key,
                              })
                                .then((blob) => downloadBlob(blob, 'suspenses_preview.xlsx'))
                                .catch((e: unknown) => notifications.show({ title: 'Ошибка экспорта', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' }))
                            }
                          >
                            <IconDownload size={14} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
          <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {totalCount.toLocaleString('ru-RU')}</Text>
            <Group gap="sm">
              <PageSizeSelect value={pageSize} onChange={handlePageSizeChange} />
              <Pagination value={page} onChange={handlePageChange} total={Math.max(1, totalPages)} size="sm" />
            </Group>
          </Group>
        </Paper>
      ) : null}

      {/* Confirm commit modal */}
      <Modal
        opened={confirmOpened}
        onClose={closeCommitDialog}
        title={committedGroupId ? 'Группа зафиксирована' : 'Зафиксировать группу'}
        centered
        radius="md"
      >
        {committedGroupId ? (
          <Stack gap="md">
            <Alert color="green" icon={<IconCircleCheck size={16} />}>
              Группа <strong>#{committedGroupId}</strong> создана из <strong>{commitTarget?.count}</strong> строк
            </Alert>
            <Group justify="flex-end">
              <Button
                variant="light"
                leftSection={<IconDownload size={14} />}
                loading={commitExporting}
                onClick={handleExportCommitted}
              >
                Скачать Excel
              </Button>
              <Button color="indigo" onClick={closeCommitDialog}>Закрыть</Button>
            </Group>
          </Stack>
        ) : (
          <Stack gap="md">
            <Text size="sm">
              Создать группу из <strong>{commitTarget?.count}</strong> строк со следующими ключами:
            </Text>
            <Paper withBorder p="sm" radius="md">
              <Stack gap={4}>
                {commitTarget && Object.entries(commitTarget.key).map(([k, v]) => (
                  <Group key={k} justify="space-between">
                    <Text size="sm" c="dimmed">{colLabel(k)}:</Text>
                    <Text size="sm" fw={500}>{v || '—'}</Text>
                  </Group>
                ))}
              </Stack>
            </Paper>
            <Group justify="flex-end">
              <Button variant="subtle" color="gray" onClick={closeCommitDialog}>Отмена</Button>
              <Button
                variant="light"
                color="green"
                leftSection={<IconDownload size={14} />}
                loading={commitLoading}
                onClick={() => handleCommit(true)}
              >
                Создать и скачать Excel
              </Button>
              <Button color="indigo" loading={commitLoading} onClick={() => handleCommit(false)}>
                Создать группу
              </Button>
            </Group>
          </Stack>
        )}
      </Modal>
    </Stack>
  );
}
