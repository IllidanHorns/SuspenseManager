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
  Modal,
  SimpleGrid,
  ThemeIcon,
  Skeleton,
  Switch,
} from '@mantine/core';
import { IconAlertCircle, IconEye, IconRefresh, IconList, IconSearch, IconTrash, IconX, IconAlertTriangle, IconLock, IconCheck, IconClock, IconChartBar, IconInfoCircle } from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { archiveSuspense, getSuspenses, getUngroupedSuspenses } from '../api/suspenses';
import { useSearchParams } from 'react-router-dom';
import { StatusBadge } from '../components/common/StatusBadge';
import { PageSizeSelect } from '../components/common/PageSizeSelect';
import { ResizableTh } from '../components/common/ResizableTh';
import { CollapsibleFilters } from '../components/common/CollapsibleFilters';
import { notifications } from '@mantine/notifications';
import { useDisclosure } from '@mantine/hooks';
import type { SuspenseLine } from '../types';
import { apiGet } from '../api/client';
import { useDefaultPageSize } from '../hooks/useDefaultPageSize';

interface FilterValues {
  isrc: string;
  artist: string;
  operator: string;
  territory: string;
}

const EMPTY_FILTERS: FilterValues = { isrc: '', artist: '', operator: '', territory: '' };

interface SuspenseKpiDto {
  totalSuspenses: number;
  noProductCount: number;
  noRightsCount: number;
  validatedCount: number;
  inGroupNoProduct: number;
  inGroupNoRights: number;
  postponedCount: number;
  backOfficeCount: number;
}

function buildFilters(v: FilterValues): Record<string, string> {
  const f: Record<string, string> = {};
  if (v.isrc.trim())      f['Isrc_contains']          = v.isrc.trim();
  if (v.artist.trim())    f['Artist_contains']         = v.artist.trim();
  if (v.operator.trim())  f['Operator_contains']       = v.operator.trim();
  if (v.territory.trim()) f['TerritoryCode_contains']  = v.territory.trim();
  return f;
}

export function SuspensesPage() {
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const mode = searchParams.get('status') ?? 'all';
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useDefaultPageSize();
  const [kpiOpened, setKpiOpened] = useState(false);
  const [archiveTarget, setArchiveTarget] = useState<number | null>(null);
  const [detailTarget, setDetailTarget] = useState<SuspenseLine | null>(null);
  const [archiveConfirmOpened, { open: openArchiveConfirm, close: closeArchiveConfirm }] = useDisclosure(false);
  const [detailOpened, { open: openDetail, close: closeDetail }] = useDisclosure(false);

  const [pending, setPending] = useState<FilterValues>(EMPTY_FILTERS);
  const [applied, setApplied] = useState<Record<string, string>>({});
  const [onlyMine, setOnlyMine] = useState(false);

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

  const handlePageSizeChange = (v: number) => { setPageSize(v); setPage(1); };

  const onKey = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') applyFilters();
  };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['suspenses', mode, page, pageSize, applied, mode === 'all' ? onlyMine : false],
    queryFn: () => {
      const params =
        mode === 'all'
          ? { pageNumber: page, pageSize, Filters: applied, ...(onlyMine ? { onlyMine: true } : {}) }
          : { pageNumber: page, pageSize, Filters: applied };
      if (mode === '0') return getUngroupedSuspenses(0, params);
      if (mode === '1') return getUngroupedSuspenses(1, params);
      return getSuspenses(params);
    },
  });
  const { data: kpi, isLoading: kpiLoading } = useQuery<SuspenseKpiDto>({
    queryKey: ['suspenses-kpi'],
    queryFn: () => apiGet<SuspenseKpiDto>('/analytics/dashboard'),
  });

  const rows = data?.items ?? [];
  const isUngroupedMode = mode === '0' || mode === '1';
  const activeFiltersCount = Object.keys(applied).length;
  const noProductPct = kpi?.totalSuspenses ? ((kpi.noProductCount / kpi.totalSuspenses) * 100) : 0;
  const noRightsPct = kpi?.totalSuspenses ? ((kpi.noRightsCount / kpi.totalSuspenses) * 100) : 0;
  const validatedPct = kpi?.totalSuspenses ? ((kpi.validatedCount / kpi.totalSuspenses) * 100) : 0;
  const pageFillPct = pageSize > 0 ? (rows.length / pageSize) * 100 : 0;

  const archiveMutation = useMutation({
    mutationFn: (id: number) => archiveSuspense(id),
    onSuccess: () => {
      notifications.show({ title: 'Суспенс удален', message: 'Строка успешно удалена', color: 'green' });
      queryClient.invalidateQueries({ queryKey: ['suspenses'] });
      closeArchiveConfirm();
      setArchiveTarget(null);
    },
    onError: (e: Error) => {
      notifications.show({ title: 'Ошибка архивации', message: e.message, color: 'red' });
    },
  });

  const canArchive = (businessStatus: number) => businessStatus === 0 || businessStatus === 1;

  const openArchiveModal = (id: number) => {
    setArchiveTarget(id);
    openArchiveConfirm();
  };

  const handleArchiveConfirm = () => {
    if (!archiveTarget) return;
    archiveMutation.mutate(archiveTarget);
  };

  const openDetailModal = (suspense: SuspenseLine) => {
    setDetailTarget(suspense);
    openDetail();
  };

  return (
    <Stack gap="xl">
      <Box>
        <Title order={3} fw={600}>Суспенс-строки</Title>
        <Text c="dimmed" size="sm">Отдельные строки суспенсов из стриминговых отчётов</Text>
      </Box>

      <Group justify="space-between" wrap="wrap" gap="md">
        <SegmentedControl
          value={mode}
          onChange={(v) => {
            setPage(1);
            setApplied({});
            setPending(EMPTY_FILTERS);
            setOnlyMine(false);
            setSearchParams({ status: v });
          }}
          data={[
            { label: 'Все', value: 'all' },
            { label: 'Нет продукта (0)', value: '0' },
            { label: 'Нет прав (1)', value: '1' },
          ]}
        />
        <Group gap="xs" wrap="wrap">
          {mode === 'all' && (
            <Switch
              label="Только мои"
              checked={onlyMine}
              onChange={(e) => {
                setOnlyMine(e.currentTarget.checked);
                setPage(1);
              }}
              size="sm"
            />
          )}
          <Button
            size="xs"
            variant="light"
            color="indigo"
            leftSection={<IconInfoCircle size={14} />}
            onClick={() => setKpiOpened(true)}
          >
            KPI-информация
          </Button>
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

      <CollapsibleFilters activeCount={Object.keys(applied).length}>
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
      </CollapsibleFilters>

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
                  <ResizableTh>ID</ResizableTh>
                  <ResizableTh>Статус</ResizableTh>
                  <ResizableTh>ISRC</ResizableTh>
                  <ResizableTh>Исполнитель</ResizableTh>
                  <ResizableTh>Трек</ResizableTh>
                  <ResizableTh>Оператор</ResizableTh>
                  <ResizableTh>Территория</ResizableTh>
                  <ResizableTh>Отправитель</ResizableTh>
                  <ResizableTh>Qty</ResizableTh>
                  <ResizableTh>Курс обмена</ResizableTh>
                  <ResizableTh>Группа</ResizableTh>
                  {isUngroupedMode && <ResizableTh>Действия</ResizableTh>}
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
                    <Table.Td><Text size="sm">{s.exchangeRate}</Text></Table.Td>
                    <Table.Td>
                      {s.groupId
                        ? <Badge variant="light" color="blue" size="sm">#{s.groupId}</Badge>
                        : <Text size="sm" c="dimmed">—</Text>}
                    </Table.Td>
                    {isUngroupedMode && (
                      <Table.Td>
                        <Group gap="xs">
                          <Tooltip label="Открыть детали">
                            <ActionIcon
                              variant="light"
                              color="indigo"
                              onClick={() => openDetailModal(s)}
                              aria-label="Открыть детали"
                            >
                              <IconEye size={16} />
                            </ActionIcon>
                          </Tooltip>
                          {canArchive(s.businessStatus) ? (
                            <Tooltip label="Удалить строку">
                              <ActionIcon
                                variant="light"
                                color="red"
                                onClick={() => openArchiveModal(s.id)}
                                loading={archiveMutation.isPending}
                                aria-label="Удалить строку"
                              >
                                <IconTrash size={16} />
                              </ActionIcon>
                            </Tooltip>
                          ) : (
                            <Text size="sm" c="dimmed">—</Text>
                          )}
                        </Group>
                      </Table.Td>
                    )}
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
          <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {data!.totalCount.toLocaleString('ru-RU')}</Text>
            <Group gap="sm">
              <PageSizeSelect value={pageSize} onChange={handlePageSizeChange} />
              <Pagination value={page} onChange={setPage} total={Math.max(1, data!.totalPages)} size="sm" />
            </Group>
          </Group>
        </Paper>
      )}

      <Modal opened={archiveConfirmOpened} onClose={closeArchiveConfirm} title="Удалить суспенс-строку" centered radius="md">
        <Stack gap="md">
          <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">
            Строка #{archiveTarget} будет удалена и исчезнет из активного списка. Это действие нельзя отменить.
          </Alert>
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={closeArchiveConfirm}>Отмена</Button>
            <Button color="red" loading={archiveMutation.isPending} onClick={handleArchiveConfirm}>Удалить</Button>
          </Group>
        </Stack>
      </Modal>

      <Modal opened={kpiOpened} onClose={() => setKpiOpened(false)} title="KPI-информация по суспенсам" centered size="xl">
        <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}>
              <Text size="xs" c="dimmed" tt="uppercase" fw={600}>Всего суспенсов</Text>
              <ThemeIcon size={28} radius="md" color="indigo" variant="light"><IconChartBar size={16} /></ThemeIcon>
            </Group>
            {kpiLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{(kpi?.totalSuspenses ?? 0).toLocaleString('ru-RU')}</Text>}
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Нет продукта</Text><ThemeIcon size={28} radius="md" color="orange" variant="light"><IconAlertTriangle size={16} /></ThemeIcon></Group>
            {kpiLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{(kpi?.noProductCount ?? 0).toLocaleString('ru-RU')}</Text>}
            <Text size="xs" c="dimmed">{noProductPct.toFixed(1)}% от всех</Text>
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Нет прав</Text><ThemeIcon size={28} radius="md" color="red" variant="light"><IconLock size={16} /></ThemeIcon></Group>
            {kpiLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{(kpi?.noRightsCount ?? 0).toLocaleString('ru-RU')}</Text>}
            <Text size="xs" c="dimmed">{noRightsPct.toFixed(1)}% от всех</Text>
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Ожидают обработки</Text><ThemeIcon size={28} radius="md" color="yellow" variant="light"><IconClock size={16} /></ThemeIcon></Group>
            {kpiLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{((kpi?.noProductCount ?? 0) + (kpi?.noRightsCount ?? 0)).toLocaleString('ru-RU')}</Text>}
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>В группах (15/16)</Text><ThemeIcon size={28} radius="md" color="blue" variant="light"><IconList size={16} /></ThemeIcon></Group>
            {kpiLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{((kpi?.inGroupNoProduct ?? 0) + (kpi?.inGroupNoRights ?? 0)).toLocaleString('ru-RU')}</Text>}
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Отложено + Back Office</Text><ThemeIcon size={28} radius="md" color="grape" variant="light"><IconAlertCircle size={16} /></ThemeIcon></Group>
            {kpiLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{((kpi?.postponedCount ?? 0) + (kpi?.backOfficeCount ?? 0)).toLocaleString('ru-RU')}</Text>}
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Валидировано</Text><ThemeIcon size={28} radius="md" color="green" variant="light"><IconCheck size={16} /></ThemeIcon></Group>
            {kpiLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{(kpi?.validatedCount ?? 0).toLocaleString('ru-RU')}</Text>}
            <Text size="xs" c="dimmed">{validatedPct.toFixed(1)}% от всех</Text>
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Text size="xs" c="dimmed" tt="uppercase" fw={600}>Текущий режим</Text>
            <Text size="lg" fw={700}>{mode === 'all' ? 'Все строки' : mode === '0' ? 'Нет продукта (0)' : 'Нет прав (1)'}</Text>
            <Text size="xs" c="dimmed">Активных фильтров: {activeFiltersCount}</Text>
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Text size="xs" c="dimmed" tt="uppercase" fw={600}>По текущей выдаче</Text>
            <Text size="lg" fw={700}>{(data?.totalCount ?? 0).toLocaleString('ru-RU')}</Text>
            <Text size="xs" c="dimmed">Строк найдено</Text>
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Text size="xs" c="dimmed" tt="uppercase" fw={600}>Заполнение страницы</Text>
            <Text size="lg" fw={700}>{pageFillPct.toFixed(0)}%</Text>
            <Text size="xs" c="dimmed">{rows.length} из {pageSize} строк</Text>
          </Paper>
        </SimpleGrid>
      </Modal>

      <Modal opened={detailOpened} onClose={closeDetail} title="Детали суспенс-строки" centered radius="md" size="lg">
        <Stack gap="xs">
          <Group justify="space-between"><Text c="dimmed">ID</Text><Text fw={600}>{detailTarget?.id ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Статус</Text><StatusBadge status={detailTarget?.businessStatus ?? 0} /></Group>
          <Group justify="space-between"><Text c="dimmed">ISRC</Text><Text>{detailTarget?.isrc ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Barcode</Text><Text>{detailTarget?.barcode ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Catalog Number</Text><Text>{detailTarget?.catalogNumber ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Формат</Text><Text>{detailTarget?.productFormatCode ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Исполнитель</Text><Text>{detailTarget?.artist ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Трек</Text><Text>{detailTarget?.trackTitle ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Жанр</Text><Text>{detailTarget?.genre ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Оператор</Text><Text>{detailTarget?.operator ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Отправитель</Text><Text>{detailTarget?.senderCompany ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Получатель</Text><Text>{detailTarget?.recipientCompany ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Тип договора</Text><Text>{detailTarget?.agreementType ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Номер договора</Text><Text>{detailTarget?.agreementNumber ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Территория</Text><Text>{detailTarget?.territoryCode ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Количество</Text><Text>{detailTarget?.qty ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">PPD</Text><Text>{detailTarget?.ppd ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Валюта</Text><Text>{detailTarget?.exchangeCurrency ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Курс</Text><Text>{detailTarget?.exchangeRate ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Группа</Text><Text>{detailTarget?.groupId ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Продукт</Text><Text>{detailTarget?.productId ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Создано</Text><Text>{detailTarget?.createTime ?? '—'}</Text></Group>
          <Group justify="space-between"><Text c="dimmed">Изменено</Text><Text>{detailTarget?.changeTime ?? '—'}</Text></Group>
        </Stack>
      </Modal>
    </Stack>
  );
}
