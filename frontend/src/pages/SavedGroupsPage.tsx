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
  NumberInput,
  Modal,
  SimpleGrid,
  ThemeIcon,
  Skeleton,
  Switch,
} from '@mantine/core';
import {
  IconFolderOpen,
  IconAlertCircle,
  IconRefresh,
  IconEye,
  IconDownload,
  IconSearch,
  IconX,
  IconChartBar,
  IconListDetails,
  IconCoin,
  IconHash,
  IconInfoCircle,
  IconPercentage,
} from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { getNoProductGroups, getNoRightsGroups, type GroupListRequest } from '../api/groups';
import { exportGroupsByStatus } from '../api/processing';
import { downloadBlob, fmtDate, fmtDateTime } from '../utils/format';
import { StatusBadge } from '../components/common/StatusBadge';
import { PageSizeSelect } from '../components/common/PageSizeSelect';
import { ResizableTh } from '../components/common/ResizableTh';
import { CollapsibleFilters } from '../components/common/CollapsibleFilters';
import { notifications } from '@mantine/notifications';
import { useDefaultPageSize } from '../hooks/useDefaultPageSize';
import { usePermissions } from '../hooks/usePermissions';
import { PermissionCodes } from '../utils/permissions';
import type { SuspenseGroup } from '../types';

function val(v: string | null | undefined) {
  return v ?? '—';
}

function fmtRevenue(v: number | undefined) {
  if (v === undefined || v === null) return '—';
  return v.toLocaleString('ru-RU', { maximumFractionDigits: 2 });
}

function NoProductRows({ groups, navigate }: { groups: SuspenseGroup[]; navigate: (p: string) => void }) {
  return (
    <>
      {groups.map((g) => (
        <Table.Tr key={g.id}>
          <Table.Td><Text size="sm" fw={600} c="indigo">#{g.id}</Text></Table.Td>
          <Table.Td><StatusBadge status={g.businessStatus} /></Table.Td>
          <Table.Td>
            <Badge variant="light" color="blue" size="sm">{g.suspenseCount ?? '—'}</Badge>
          </Table.Td>
          <Table.Td><Text size="sm">{fmtRevenue(g.revenueRub)}</Text></Table.Td>
          <Table.Td><Text size="sm">{val(g.groupMetaData?.artist)}</Text></Table.Td>
          <Table.Td><Text size="sm">{val(g.groupMetaData?.title)}</Text></Table.Td>
          <Table.Td><Text size="xs" ff="monospace">{val(g.groupMetaData?.isrc)}</Text></Table.Td>
          <Table.Td><Text size="xs">{val(g.groupMetaData?.barcode)}</Text></Table.Td>
          <Table.Td><Text size="xs" c="dimmed">{val(g.groupMetaData?.genre)}</Text></Table.Td>
          <Table.Td><Text size="xs" c="dimmed">{fmtDateTime(g.createTime)}</Text></Table.Td>
          <Table.Td>
            <Group justify="center">
              <Tooltip label="Открыть группу">
                <ActionIcon variant="light" color="indigo" size="sm" onClick={() => navigate(`/groups/${g.id}`)}>
                  <IconEye size={14} />
                </ActionIcon>
              </Tooltip>
            </Group>
          </Table.Td>
        </Table.Tr>
      ))}
    </>
  );
}

function NoRightsRows({ groups, navigate }: { groups: SuspenseGroup[]; navigate: (p: string) => void }) {
  return (
    <>
      {groups.map((g) => {
        const rights = g.groupMetaRights;
        const period = rights?.docStart || rights?.docEnd
          ? `${fmtDate(rights.docStart)} – ${fmtDate(rights.docEnd)}`
          : '—';

        return (
          <Table.Tr key={g.id}>
            <Table.Td><Text size="sm" fw={600} c="indigo">#{g.id}</Text></Table.Td>
            <Table.Td><StatusBadge status={g.businessStatus} /></Table.Td>
            <Table.Td>
              <Badge variant="light" color="blue" size="sm">{g.suspenseCount ?? '—'}</Badge>
            </Table.Td>
            <Table.Td><Text size="sm">{fmtRevenue(g.revenueRub)}</Text></Table.Td>
            <Table.Td><Text size="sm">{val(g.groupMetaData?.title ?? g.catalogProduct?.productName)}</Text></Table.Td>
            <Table.Td><Text size="sm">{val(g.groupMetaData?.artist ?? g.catalogProduct?.artist)}</Text></Table.Td>
            <Table.Td><Text size="xs" ff="monospace">{val(g.groupMetaData?.isrc ?? g.catalogProduct?.isrc)}</Text></Table.Td>
            <Table.Td><Text size="xs">{val(rights?.docNumber)}</Text></Table.Td>
            <Table.Td><Text size="xs">{val(rights?.territoryCode)}</Text></Table.Td>
            <Table.Td><Text size="xs" c="dimmed">{period}</Text></Table.Td>
            <Table.Td>
              <Text size="xs">{rights?.share != null ? `${rights.share}%` : '—'}</Text>
            </Table.Td>
            <Table.Td><Text size="xs" c="dimmed">{fmtDateTime(g.createTime)}</Text></Table.Td>
            <Table.Td>
              <Group justify="center">
                <Tooltip label="Открыть группу">
                  <ActionIcon variant="light" color="indigo" size="sm" onClick={() => navigate(`/groups/${g.id}`)}>
                    <IconEye size={14} />
                  </ActionIcon>
                </Tooltip>
              </Group>
            </Table.Td>
          </Table.Tr>
        );
      })}
    </>
  );
}

function GroupTable({
  groups, type, loading, error, page, pageSize, totalPages, totalCount, onPageChange, onPageSizeChange,
}: {
  groups: SuspenseGroup[];
  type: 'no-product' | 'no-rights';
  loading: boolean;
  error: string;
  page: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (p: number) => void;
  onPageSizeChange: (v: number) => void;
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
          {type === 'no-product' ? (
            <Table.Tr>
              <ResizableTh>ID</ResizableTh>
              <ResizableTh>Статус</ResizableTh>
              <ResizableTh>Строк</ResizableTh>
              <ResizableTh>Выручка, ₽</ResizableTh>
              <ResizableTh>Исполнитель</ResizableTh>
              <ResizableTh>Трек</ResizableTh>
              <ResizableTh>ISRC</ResizableTh>
              <ResizableTh>Баркод</ResizableTh>
              <ResizableTh>Жанр</ResizableTh>
              <ResizableTh>Создана</ResizableTh>
              <ResizableTh style={{ textAlign: 'center' }}>Действия</ResizableTh>
            </Table.Tr>
          ) : (
            <Table.Tr>
              <ResizableTh>ID</ResizableTh>
              <ResizableTh>Статус</ResizableTh>
              <ResizableTh>Строк</ResizableTh>
              <ResizableTh>Выручка, ₽</ResizableTh>
              <ResizableTh>Продукт</ResizableTh>
              <ResizableTh>Исполнитель</ResizableTh>
              <ResizableTh>ISRC</ResizableTh>
              <ResizableTh>Договор</ResizableTh>
              <ResizableTh>Территория</ResizableTh>
              <ResizableTh>Период</ResizableTh>
              <ResizableTh>Доля</ResizableTh>
              <ResizableTh>Создана</ResizableTh>
              <ResizableTh style={{ textAlign: 'center' }}>Действия</ResizableTh>
            </Table.Tr>
          )}
        </Table.Thead>
        <Table.Tbody>
          {type === 'no-product'
            ? <NoProductRows groups={groups} navigate={navigate} />
            : <NoRightsRows groups={groups} navigate={navigate} />
          }
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
          <Group gap="sm">
            <PageSizeSelect value={pageSize} onChange={onPageSizeChange} />
            <Pagination value={page} onChange={onPageChange} total={Math.max(1, totalPages)} size="sm" />
          </Group>
        </Group>
      )}
    </>
  );
}

// Pending filter state

interface FilterState {
  artist: string;
  title: string;
  isrc: string;
  barcode: string;
  territoryCode: string;
  docNumber: string;
  countMin: number | '';
  countMax: number | '';
  revenueMin: number | '';
  revenueMax: number | '';
}

const EMPTY_FILTER: FilterState = {
  artist: '', title: '', isrc: '', barcode: '', territoryCode: '', docNumber: '',
  countMin: '', countMax: '', revenueMin: '', revenueMax: '',
};

function filterStateToRequest(f: FilterState): Partial<GroupListRequest> {
  const r: Partial<GroupListRequest> = {};
  if (f.artist) r.artist = f.artist;
  if (f.title) r.title = f.title;
  if (f.isrc) r.isrc = f.isrc;
  if (f.barcode) r.barcode = f.barcode;
  if (f.territoryCode) r.territoryCode = f.territoryCode;
  if (f.docNumber) r.docNumber = f.docNumber;
  if (f.countMin !== '') r.countMin = Number(f.countMin);
  if (f.countMax !== '') r.countMax = Number(f.countMax);
  if (f.revenueMin !== '') r.revenueMin = Number(f.revenueMin);
  if (f.revenueMax !== '') r.revenueMax = Number(f.revenueMax);
  return r;
}

function hasActiveFilters(f: FilterState) {
  return Object.values(f).some((v) => v !== '' && v !== null && v !== undefined);
}

// TabContent

function TabContent({ type }: { type: 'no-product' | 'no-rights' }) {
  const { hasPermission } = usePermissions();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useDefaultPageSize();
  const [kpiOpened, setKpiOpened] = useState(false);
  const [onlyMine, setOnlyMine] = useState(false);
  const status = type === 'no-product' ? 15 : 16;

  const [pending, setPending] = useState<FilterState>(EMPTY_FILTER);
  const [applied, setApplied] = useState<Partial<GroupListRequest>>({});

  const handlePageSizeChange = (v: number) => { setPageSize(v); setPage(1); };

  const applyFilters = () => {
    setApplied(filterStateToRequest(pending));
    setPage(1);
  };

  const resetFilters = () => {
    setPending(EMPTY_FILTER);
    setApplied({});
    setOnlyMine(false);
    setPage(1);
  };

  const active = hasActiveFilters(pending);

  const listParams = {
    pageNumber: page,
    pageSize,
    ...applied,
    ...(onlyMine ? { onlyMine: true as const } : {}),
  };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['groups', type, page, pageSize, applied, onlyMine],
    queryFn: () =>
      type === 'no-product' ? getNoProductGroups(listParams) : getNoRightsGroups(listParams),
  });

  const handleExport = async () => {
    try {
      const blob = await exportGroupsByStatus(status as 15 | 16);
      downloadBlob(blob, `groups_status_${status}.xlsx`);
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка экспорта', color: 'red' });
    }
  };

  const set = (field: keyof FilterState) => (val: string | number) =>
    setPending((p) => ({ ...p, [field]: val }));

  const handleEnter = (e: React.KeyboardEvent) => { if (e.key === 'Enter') applyFilters(); };
  const items = data?.items ?? [];
  const groupsOnPage = items.length;
  const linesOnPage = items.reduce((sum, g) => sum + (g.suspenseCount ?? 0), 0);
  const revenueOnPage = items.reduce((sum, g) => sum + (g.revenueRub ?? 0), 0);
  const avgLines = groupsOnPage > 0 ? linesOnPage / groupsOnPage : 0;
  const maxLines = groupsOnPage > 0 ? Math.max(...items.map((g) => g.suspenseCount ?? 0)) : 0;
  const avgRevenuePerGroup = groupsOnPage > 0 ? revenueOnPage / groupsOnPage : 0;
  const metadataCoverage = groupsOnPage > 0
    ? (items.filter((g) =>
      type === 'no-product'
        ? !!(g.groupMetaData?.title || g.groupMetaData?.artist || g.groupMetaData?.isrc)
        : !!(g.groupMetaRights?.docNumber || g.groupMetaRights?.territoryCode || g.groupMetaRights?.share)
    ).length / groupsOnPage) * 100
    : 0;

  return (
    <Stack gap="md">
      <Group justify="space-between" gap="xs" wrap="wrap">
        <Group gap="md">
          <Switch
            label="Только мои"
            checked={onlyMine}
            onChange={(e) => {
              setOnlyMine(e.currentTarget.checked);
              setPage(1);
            }}
            size="sm"
          />
          <Group gap="xs">
            <Tooltip label="Обновить">
              <ActionIcon variant="light" color="gray" onClick={() => refetch()}>
                <IconRefresh size={16} />
              </ActionIcon>
            </Tooltip>
            <Button size="xs" variant="light" color="indigo" leftSection={<IconInfoCircle size={14} />} onClick={() => setKpiOpened(true)}>
              KPI-информация
            </Button>
          </Group>
        </Group>
        {hasPermission(PermissionCodes.groupsExport) && (
          <Button size="xs" variant="light" color="green" leftSection={<IconDownload size={14} />} onClick={handleExport}>
            Экспорт
          </Button>
        )}
      </Group>

      <CollapsibleFilters activeCount={Object.keys(applied).length}>
        <Paper withBorder radius="md" p="sm">
          <Stack gap="xs">
            <Group gap="sm" align="flex-end" wrap="wrap">
              <TextInput
                size="xs" label="Исполнитель" placeholder="Поиск..." style={{ width: 160 }}
                value={pending.artist} onChange={(e) => set('artist')(e.target.value)} onKeyDown={handleEnter}
              />
              <TextInput
                size="xs" label="Трек / Продукт" placeholder="Поиск..." style={{ width: 160 }}
                value={pending.title} onChange={(e) => set('title')(e.target.value)} onKeyDown={handleEnter}
              />
              <TextInput
                size="xs" label="ISRC" placeholder="Поиск..." style={{ width: 150 }}
                value={pending.isrc} onChange={(e) => set('isrc')(e.target.value)} onKeyDown={handleEnter}
              />
              <TextInput
                size="xs" label="Баркод" placeholder="Поиск..." style={{ width: 140 }}
                value={pending.barcode} onChange={(e) => set('barcode')(e.target.value)} onKeyDown={handleEnter}
              />
              {type === 'no-rights' && (
                <>
                  <TextInput
                    size="xs" label="Территория" placeholder="RU, US..." style={{ width: 120 }}
                    value={pending.territoryCode} onChange={(e) => set('territoryCode')(e.target.value)} onKeyDown={handleEnter}
                  />
                  <TextInput
                    size="xs" label="№ договора" placeholder="Поиск..." style={{ width: 140 }}
                    value={pending.docNumber} onChange={(e) => set('docNumber')(e.target.value)} onKeyDown={handleEnter}
                  />
                </>
              )}
            </Group>
            <Group gap="sm" align="flex-end" wrap="wrap">
              <NumberInput
                size="xs" label="Строк от" placeholder="0" min={0} style={{ width: 100 }}
                value={pending.countMin} onChange={(v) => set('countMin')(v as number)}
              />
              <NumberInput
                size="xs" label="Строк до" placeholder="∞" min={0} style={{ width: 100 }}
                value={pending.countMax} onChange={(v) => set('countMax')(v as number)}
              />
              <NumberInput
                size="xs" label="Выручка от, ₽" placeholder="0" min={0} style={{ width: 130 }}
                value={pending.revenueMin} onChange={(v) => set('revenueMin')(v as number)}
              />
              <NumberInput
                size="xs" label="Выручка до, ₽" placeholder="∞" min={0} style={{ width: 130 }}
                value={pending.revenueMax} onChange={(v) => set('revenueMax')(v as number)}
              />
              <Group gap="xs" style={{ alignSelf: 'flex-end' }}>
                <Button size="xs" leftSection={<IconSearch size={12} />} onClick={applyFilters}>
                  Найти
                </Button>
                {active && (
                  <Button size="xs" variant="subtle" color="gray" leftSection={<IconX size={12} />} onClick={resetFilters}>
                    Сбросить
                  </Button>
                )}
              </Group>
            </Group>
          </Stack>
        </Paper>
      </CollapsibleFilters>

      <Paper withBorder radius="md">
        <GroupTable
          groups={data?.items ?? []}
          type={type}
          loading={isLoading}
          error={error?.message ?? ''}
          page={page}
          pageSize={pageSize}
          totalPages={data?.totalPages ?? 1}
          totalCount={data?.totalCount ?? 0}
          onPageChange={setPage}
          onPageSizeChange={handlePageSizeChange}
        />
      </Paper>

      <Modal opened={kpiOpened} onClose={() => setKpiOpened(false)} title={`KPI по группам (${type === 'no-product' ? 'статус 15' : 'статус 16'})`} centered size="xl">
        <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Всего групп (выборка)</Text><ThemeIcon size={28} radius="md" color="indigo" variant="light"><IconHash size={16} /></ThemeIcon></Group>
            {isLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{(data?.totalCount ?? 0).toLocaleString('ru-RU')}</Text>}
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Групп на странице</Text><ThemeIcon size={28} radius="md" color="blue" variant="light"><IconListDetails size={16} /></ThemeIcon></Group>
            {isLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{groupsOnPage.toLocaleString('ru-RU')}</Text>}
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Строк в группах (страница)</Text><ThemeIcon size={28} radius="md" color="cyan" variant="light"><IconListDetails size={16} /></ThemeIcon></Group>
            {isLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{linesOnPage.toLocaleString('ru-RU')}</Text>}
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Выручка (страница), ₽</Text><ThemeIcon size={28} radius="md" color="green" variant="light"><IconCoin size={16} /></ThemeIcon></Group>
            {isLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{revenueOnPage.toLocaleString('ru-RU', { maximumFractionDigits: 0 })}</Text>}
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Среднее строк/группа</Text><ThemeIcon size={28} radius="md" color="violet" variant="light"><IconChartBar size={16} /></ThemeIcon></Group>
            {isLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{avgLines.toLocaleString('ru-RU', { maximumFractionDigits: 1 })}</Text>}
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Макс. строк в группе</Text><ThemeIcon size={28} radius="md" color="orange" variant="light"><IconHash size={16} /></ThemeIcon></Group>
            {isLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{maxLines.toLocaleString('ru-RU')}</Text>}
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Ср. выручка/группа, ₽</Text><ThemeIcon size={28} radius="md" color="teal" variant="light"><IconCoin size={16} /></ThemeIcon></Group>
            {isLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{avgRevenuePerGroup.toLocaleString('ru-RU', { maximumFractionDigits: 0 })}</Text>}
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Group justify="space-between" mb={6}><Text size="xs" c="dimmed" tt="uppercase" fw={600}>Заполнение метаданных</Text><ThemeIcon size={28} radius="md" color="grape" variant="light"><IconPercentage size={16} /></ThemeIcon></Group>
            {isLoading ? <Skeleton height={26} width={80} /> : <Text size="xl" fw={700}>{metadataCoverage.toFixed(0)}%</Text>}
            <Text size="xs" c="dimmed">{type === 'no-product' ? 'title/artist/isrc' : 'договор/территория/доля'}</Text>
          </Paper>
          <Paper withBorder radius="md" p="md">
            <Text size="xs" c="dimmed" tt="uppercase" fw={600}>Текущий контекст</Text>
            <Text size="lg" fw={700}>{type === 'no-product' ? 'Нет продукта (15)' : 'Нет прав (16)'}</Text>
            <Text size="xs" c="dimmed">Активных фильтров: {Object.keys(applied).length}</Text>
          </Paper>
        </SimpleGrid>
      </Modal>
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
