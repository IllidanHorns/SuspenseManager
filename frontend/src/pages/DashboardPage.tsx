import { useEffect, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { DatePickerInput } from '@mantine/dates';
import {
  Grid,
  Card,
  Text,
  Title,
  Group,
  Stack,
  SimpleGrid,
  Button,
  Skeleton,
  ThemeIcon,
  Box,
  Paper,
  Alert,
  ActionIcon,
  Tooltip,
} from '@mantine/core';
import { DonutChart, BarChart } from '@mantine/charts';
import {
  IconAlertTriangle,
  IconLock,
  IconFolderOpen,
  IconClock,
  IconBuildingWarehouse,
  IconLayersSubtract,
  IconCheck,
  IconAlertCircle,
  IconDatabase,
  IconChartBar,
  IconFileTypePdf,
  IconCalendar,
  IconX,
} from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import { apiGet } from '../api/client';
import { STATUS_LABELS, STATUS_COLORS } from '../types';

// Types

type DRange = [Date | null, Date | null];

interface OperatorStat { operator: string; count: number; revenue: number; }
interface StatusCount { status: number; count: number; }
interface TerritoryStatDto { territoryCode: string; count: number; revenue: number; }
interface CompanyStatDto { companyName: string; count: number; }

interface DashboardDto {
  totalSuspenses: number;
  noProductCount: number;
  noRightsCount: number;
  inGroupNoProduct: number;
  inGroupNoRights: number;
  validatedCount: number;
  backOfficeCount: number;
  postponedCount: number;
  totalGroups: number;
  totalProducts: number;
  totalCompanies: number;
  totalRevenue: number;
  totalStreams: number;
  topOperators: OperatorStat[];
  statusDistribution: StatusCount[];
  topTerritories: TerritoryStatDto[];
  topCompanies: CompanyStatDto[];
}

// Query helpers

function toParams(dates: DRange): Record<string, unknown> {
  const p: Record<string, unknown> = {};
  if (dates[0]) p.dateFrom = dates[0].toISOString().slice(0, 10);
  if (dates[1]) p.dateTo = dates[1].toISOString().slice(0, 10);
  return p;
}

function dateKey(dates: DRange): string {
  return `${dates[0]?.toISOString() ?? ''}_${dates[1]?.toISOString() ?? ''}`;
}

/** Если у графика задан свой диапазон — берём его, иначе наследуем глобальный */
function eff(chart: DRange, global: DRange): DRange {
  return (chart[0] !== null || chart[1] !== null) ? chart : global;
}

function buildQuery(dates: DRange) {
  return {
    queryKey: ['dashboard', dateKey(dates)] as const,
    queryFn: () => apiGet<DashboardDto>('/analytics/dashboard', toParams(dates)),
    staleTime: 2 * 60 * 1000,
    retry: false as const,
  };
}

function fmtDate(d: Date | null): string {
  if (!d) return '...';
  return d.toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric' });
}

function truncate(s: string, n: number): string {
  return s.length > n ? s.slice(0, n) + '…' : s;
}

// Small components

function ChartDateFilter({ value, onChange }: { value: DRange; onChange: (v: DRange) => void }) {
  const hasValue = value[0] !== null || value[1] !== null;
  return (
    <Group gap={4} wrap="nowrap">
      <DatePickerInput
        type="range"
        size="xs"
        placeholder="Свой период"
        value={value}
        onChange={onChange}
        clearable
        w={200}
        leftSection={<IconCalendar size={12} />}
        valueFormat="DD.MM.YY"
      />
      {hasValue && (
        <Tooltip label="Сбросить фильтр этого графика">
          <ActionIcon size="xs" variant="subtle" color="gray" onClick={() => onChange([null, null])}>
            <IconX size={12} />
          </ActionIcon>
        </Tooltip>
      )}
    </Group>
  );
}

const STAT_CARDS = [
  { key: 'noProductCount',   label: 'Нет продукта',             icon: IconAlertTriangle,    color: 'orange', path: '/suspenses?status=0' },
  { key: 'noRightsCount',    label: 'Нет прав',                 icon: IconLock,             color: 'red',    path: '/suspenses?status=1' },
  { key: 'inGroupNoProduct', label: 'В группах (нет продукта)', icon: IconFolderOpen,       color: 'blue',   path: '/groups?tab=0' },
  { key: 'inGroupNoRights',  label: 'В группах (нет прав)',     icon: IconFolderOpen,       color: 'violet', path: '/groups?tab=1' },
  { key: 'postponedCount',   label: 'Отложено',                 icon: IconClock,            color: 'yellow', path: '/postponed' },
  { key: 'backOfficeCount',  label: 'Бэк-офис',                 icon: IconBuildingWarehouse,color: 'gray',   path: '/groups' },
  { key: 'validatedCount',   label: 'Прошло валидацию',         icon: IconCheck,            color: 'green',  path: '/suspenses' },
  { key: 'totalGroups',      label: 'Групп всего',              icon: IconLayersSubtract,   color: 'cyan',   path: '/groups' },
];

function StatCard({ label, value, icon: Icon, color, onClick }: {
  label: string; value?: number; icon: typeof IconAlertTriangle; color: string; onClick: () => void;
}) {
  return (
    <Card withBorder radius="md" p="md" style={{ cursor: 'pointer' }} onClick={onClick}>
      <Group justify="space-between" mb="xs" wrap="nowrap">
        <ThemeIcon size={36} radius="md" color={color} variant="light">
          <Icon size={18} />
        </ThemeIcon>
        <Text size="xs" c="dimmed" tt="uppercase" fw={600} ta="right" style={{ lineHeight: 1.2 }}>
          {label}
        </Text>
      </Group>
      {value === undefined
        ? <Skeleton height={28} width={60} radius="sm" mt={4} />
        : <Text size="xl" fw={700}>{value.toLocaleString('ru-RU')}</Text>}
    </Card>
  );
}

// Page

export function DashboardPage() {
  const navigate = useNavigate();

  // PDF module refs
  const pdfExportRef    = useRef<typeof import('../utils/dashboardPdfExport') | null>(null);
  const htmlToImageRef  = useRef<typeof import('html-to-image') | null>(null);
  const statCardsRef    = useRef<HTMLDivElement>(null);
  const summaryRef      = useRef<HTMLDivElement>(null);
  const donutRef        = useRef<HTMLDivElement>(null);
  const opsCountRef     = useRef<HTMLDivElement>(null);
  const opsRevRef       = useRef<HTMLDivElement>(null);
  const terrRef         = useRef<HTMLDivElement>(null);
  const compRef         = useRef<HTMLDivElement>(null);
  const [pdfReady, setPdfReady] = useState(false);
  const [pdfBusy, setPdfBusy]   = useState(false);

  useEffect(() => {
    let alive = true;
    void Promise.all([import('../utils/dashboardPdfExport'), import('html-to-image')])
      .then(([mod, hti]) => {
        if (!alive) return;
        pdfExportRef.current   = mod;
        htmlToImageRef.current = hti;
        setPdfReady(true);
      })
      .catch(() => {
        notifications.show({ color: 'red', title: 'PDF', message: 'Не удалось подгрузить модуль экспорта.' });
      });
    return () => { alive = false; };
  }, []);

  // Date filter state
  const [globalDates,   setGlobalDates]   = useState<DRange>([null, null]);
  const [donutDates,    setDonutDates]    = useState<DRange>([null, null]);
  const [opsCountDates, setOpsCountDates] = useState<DRange>([null, null]);
  const [opsRevDates,   setOpsRevDates]   = useState<DRange>([null, null]);
  const [terrDates,     setTerrDates]     = useState<DRange>([null, null]);
  const [compDates,     setCompDates]     = useState<DRange>([null, null]);

  // Queries — React Query деduplicates по одному ключу, поэтому несколько графиков
  // с одинаковым диапазоном делают только один сетевой запрос
  const { data: mainStats, isLoading, error } = useQuery<DashboardDto>(buildQuery(globalDates));
  const { data: donutStats,    isLoading: donutLoad    } = useQuery<DashboardDto>(buildQuery(eff(donutDates,    globalDates)));
  const { data: opsCountStats, isLoading: opsCountLoad } = useQuery<DashboardDto>(buildQuery(eff(opsCountDates, globalDates)));
  const { data: opsRevStats,   isLoading: opsRevLoad   } = useQuery<DashboardDto>(buildQuery(eff(opsRevDates,   globalDates)));
  const { data: terrStats,     isLoading: terrLoad     } = useQuery<DashboardDto>(buildQuery(eff(terrDates,     globalDates)));
  const { data: compStats,     isLoading: compLoad     } = useQuery<DashboardDto>(buildQuery(eff(compDates,     globalDates)));

  // Chart data transforms
  const donutData = (donutStats?.statusDistribution ?? [])
    .filter(s => s.count > 0)
    .map(s => ({
      name: STATUS_LABELS[s.status] ?? `Статус ${s.status}`,
      value: s.count,
      color: STATUS_COLORS[s.status] ?? 'gray',
    }));

  const opsCountData = (opsCountStats?.topOperators ?? []).map(op => ({
    operator: truncate(op.operator, 14),
    count: op.count,
  }));

  const opsRevData = (opsRevStats?.topOperators ?? []).map(op => ({
    operator: truncate(op.operator, 14),
    revenue: Math.round(Number(op.revenue)),
  }));

  const terrData = (terrStats?.topTerritories ?? []).map(t => ({
    territory: t.territoryCode,
    count: t.count,
  }));

  const compData = (compStats?.topCompanies ?? []).map(c => ({
    company: truncate(c.companyName, 16),
    count: c.count,
  }));

  const globalRangeLabel = (globalDates[0] || globalDates[1])
    ? `${fmtDate(globalDates[0])} — ${fmtDate(globalDates[1])}`
    : null;

  // PDF export
  const handlePdf = () => {
    const mod = pdfExportRef.current;
    const hti = htmlToImageRef.current;
    if (!mainStats || !mod || !hti) return;
    setPdfBusy(true);
    const snap = async (el: HTMLElement | null) => {
      if (!el) return undefined;
      try { return await hti.toPng(el, { cacheBust: true, pixelRatio: 2, backgroundColor: '#ffffff' }); }
      catch { return undefined; }
    };
    void (async () => {
      try {
        const [statCards, summary, donut, bar, revBar, terrBar, compBar] = await Promise.all([
          snap(statCardsRef.current),
          snap(summaryRef.current),
          snap(donutRef.current),
          snap(opsCountRef.current),
          snap(opsRevRef.current),
          snap(terrRef.current),
          snap(compRef.current),
        ]);
        await mod.downloadDashboardPdf(
          mainStats,
          { statCards, summary, donut, bar, revBar, terrBar, compBar },
          {
            from: globalDates[0]?.toISOString().slice(0, 10),
            to:   globalDates[1]?.toISOString().slice(0, 10),
          }
        );
      } catch (err) {
        notifications.show({
          color: 'red', title: 'Ошибка PDF',
          message: err instanceof Error ? err.message : 'Не удалось сформировать файл',
        });
      } finally {
        setPdfBusy(false);
      }
    })();
  };

  return (
    <Stack gap="xl">

      {/* ── Header ── */}
      <Group justify="space-between" align="flex-start" wrap="wrap" gap="md">
        <Box>
          <Title order={3} fw={600}>Дашборд</Title>
          <Text c="dimmed" size="sm">Мониторинг входящих обращений</Text>
        </Box>
        <Group gap="sm" wrap="wrap">
          <DatePickerInput
            type="range"
            placeholder="Глобальный фильтр по периоду"
            value={globalDates}
            onChange={setGlobalDates}
            clearable
            w={280}
            leftSection={<IconCalendar size={16} />}
            valueFormat="DD.MM.YYYY"
          />
          <Button
            variant="light"
            color="gray"
            leftSection={<IconFileTypePdf size={18} />}
            disabled={isLoading || !mainStats || !pdfReady || pdfBusy}
            loading={pdfBusy}
            onClick={handlePdf}
          >
            Скачать PDF
          </Button>
        </Group>
      </Group>

      {globalRangeLabel && (
        <Alert color="blue" variant="light" radius="md" icon={<IconCalendar size={16} />} p="xs">
          <Text size="sm">Данные за период: <b>{globalRangeLabel}</b>. Отдельные графики могут иметь свой диапазон.</Text>
        </Alert>
      )}

      {error && (
        <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">
          Не удалось загрузить данные: {(error as Error).message}
        </Alert>
      )}

      {/* ── Stat cards ── */}
      <Box ref={statCardsRef} p="sm" style={{ borderRadius: 8 }}>
        <SimpleGrid cols={{ base: 2, sm: 3, lg: 4 }} spacing="md">
          {STAT_CARDS.map(({ key, label, icon, color, path }) => (
            <StatCard
              key={key}
              label={label}
              value={mainStats?.[key as keyof DashboardDto] as number | undefined}
              icon={icon}
              color={color}
              onClick={() => navigate(path)}
            />
          ))}
        </SimpleGrid>
      </Box>

      {/* ── Summary numbers ── */}
      {(isLoading || mainStats) && (
        <Box ref={summaryRef} p="sm" style={{ borderRadius: 8 }}>
          <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="md">
            <Paper withBorder radius="md" p="md">
              <Group gap="xs" mb={4}>
                <IconDatabase size={16} color="var(--mantine-color-dimmed)" />
                <Text size="xs" c="dimmed" tt="uppercase" fw={600}>Всего записей</Text>
              </Group>
              {isLoading
                ? <Skeleton height={28} width={80} />
                : <Text size="xl" fw={700}>{mainStats!.totalSuspenses.toLocaleString('ru-RU')}</Text>}
            </Paper>
            <Paper withBorder radius="md" p="md">
              <Group gap="xs" mb={4}>
                <IconChartBar size={16} color="var(--mantine-color-dimmed)" />
                <Text size="xs" c="dimmed" tt="uppercase" fw={600}>Стримов</Text>
              </Group>
              {isLoading
                ? <Skeleton height={28} width={80} />
                : <Text size="xl" fw={700}>{mainStats!.totalStreams.toLocaleString('ru-RU')}</Text>}
            </Paper>
            <Paper withBorder radius="md" p="md">
              <Group gap="xs" mb={4}>
                <IconCheck size={16} color="var(--mantine-color-dimmed)" />
                <Text size="xs" c="dimmed" tt="uppercase" fw={600}>Выручка к распределению</Text>
              </Group>
              {isLoading
                ? <Skeleton height={28} width={80} />
                : <Text size="xl" fw={700}>
                    {mainStats!.totalRevenue.toLocaleString('ru-RU', { maximumFractionDigits: 0 })} ₽
                  </Text>}
            </Paper>
          </SimpleGrid>
        </Box>
      )}

      {/* ── Row 1: Status donut + Operators by count ── */}
      <Grid>
        <Grid.Col span={{ base: 12, md: 5 }}>
          <Card withBorder radius="md" p="lg" h="100%">
            <Group justify="space-between" mb="md" wrap="wrap" gap="xs">
              <Title order={5}>Структура обращений по статусам</Title>
              <ChartDateFilter value={donutDates} onChange={setDonutDates} />
            </Group>
            <Box ref={donutRef}>
              {donutLoad ? (
                <Stack align="center" py="xl"><Skeleton height={200} width={200} circle /></Stack>
              ) : donutData.length === 0 ? (
                <Stack align="center" py="xl"><Text c="dimmed" size="sm">Нет данных</Text></Stack>
              ) : (
                <DonutChart
                  data={donutData}
                  withLabelsLine withLabels labelsType="value"
                  size={220} thickness={40} mx="auto"
                />
              )}
            </Box>
          </Card>
        </Grid.Col>

        <Grid.Col span={{ base: 12, md: 7 }}>
          <Card withBorder radius="md" p="lg" h="100%">
            <Group justify="space-between" mb="md" wrap="wrap" gap="xs">
              <Title order={5}>Операторы: объём входящих записей</Title>
              <ChartDateFilter value={opsCountDates} onChange={setOpsCountDates} />
            </Group>
            <Box ref={opsCountRef}>
              {opsCountLoad ? (
                <Skeleton height={220} radius="sm" />
              ) : opsCountData.length === 0 ? (
                <Stack align="center" py="xl"><Text c="dimmed" size="sm">Нет данных</Text></Stack>
              ) : (
                <BarChart
                  h={220} data={opsCountData} dataKey="operator"
                  series={[{ name: 'count', label: 'Кол-во', color: 'indigo' }]}
                  tickLine="y" gridAxis="y"
                />
              )}
            </Box>
          </Card>
        </Grid.Col>
      </Grid>

      {/* ── Row 2: Operators by revenue + Territories ── */}
      <Grid>
        <Grid.Col span={{ base: 12, md: 6 }}>
          <Card withBorder radius="md" p="lg" h="100%">
            <Group justify="space-between" mb="md" wrap="wrap" gap="xs">
              <Title order={5}>Операторы: выручка к распределению</Title>
              <ChartDateFilter value={opsRevDates} onChange={setOpsRevDates} />
            </Group>
            <Box ref={opsRevRef}>
              {opsRevLoad ? (
                <Skeleton height={220} radius="sm" />
              ) : opsRevData.length === 0 ? (
                <Stack align="center" py="xl"><Text c="dimmed" size="sm">Нет данных</Text></Stack>
              ) : (
                <BarChart
                  h={220} data={opsRevData} dataKey="operator"
                  series={[{ name: 'revenue', label: 'Выручка, ₽', color: 'teal' }]}
                  tickLine="y" gridAxis="y"
                />
              )}
            </Box>
          </Card>
        </Grid.Col>

        <Grid.Col span={{ base: 12, md: 6 }}>
          <Card withBorder radius="md" p="lg" h="100%">
            <Group justify="space-between" mb="md" wrap="wrap" gap="xs">
              <Title order={5}>Территории: входящие обращения</Title>
              <ChartDateFilter value={terrDates} onChange={setTerrDates} />
            </Group>
            <Box ref={terrRef}>
              {terrLoad ? (
                <Skeleton height={220} radius="sm" />
              ) : terrData.length === 0 ? (
                <Stack align="center" py="xl"><Text c="dimmed" size="sm">Нет данных</Text></Stack>
              ) : (
                <BarChart
                  h={220} data={terrData} dataKey="territory"
                  series={[{ name: 'count', label: 'Кол-во', color: 'orange' }]}
                  tickLine="y" gridAxis="y"
                />
              )}
            </Box>
          </Card>
        </Grid.Col>
      </Grid>

      {/* ── Row 3: Top companies (full width) ── */}
      <Card withBorder radius="md" p="lg">
        <Group justify="space-between" mb="md" wrap="wrap" gap="xs">
          <Title order={5}>Правообладатели: входящие обращения</Title>
          <ChartDateFilter value={compDates} onChange={setCompDates} />
        </Group>
        <Box ref={compRef}>
          {compLoad ? (
            <Skeleton height={220} radius="sm" />
          ) : compData.length === 0 ? (
            <Stack align="center" py="xl"><Text c="dimmed" size="sm">Нет данных</Text></Stack>
          ) : (
            <BarChart
              h={220} data={compData} dataKey="company"
              series={[{ name: 'count', label: 'Кол-во', color: 'violet' }]}
              tickLine="y" gridAxis="y"
            />
          )}
        </Box>
      </Card>

    </Stack>
  );
}
