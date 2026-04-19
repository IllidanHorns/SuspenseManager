import { useEffect, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
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
  Divider,
} from '@mantine/core';
import { DonutChart, BarChart } from '@mantine/charts';
import {
  IconAlertTriangle,
  IconLock,
  IconFolderOpen,
  IconClock,
  IconBuildingWarehouse,
  IconUpload,
  IconLayersSubtract,
  IconArrowRight,
  IconCheck,
  IconAlertCircle,
  IconDatabase,
  IconChartBar,
  IconFileTypePdf,
} from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import { apiGet } from '../api/client';
import { STATUS_LABELS, STATUS_COLORS } from '../types';

interface OperatorStat {
  operator: string;
  count: number;
  revenue: number;
}

interface StatusCount {
  status: number;
  count: number;
}

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
}

const STAT_CARDS = [
  { key: 'noProductCount',   label: 'Нет продукта',           icon: IconAlertTriangle, color: 'orange', path: '/suspenses?status=0' },
  { key: 'noRightsCount',    label: 'Нет прав',               icon: IconLock,          color: 'red',    path: '/suspenses?status=1' },
  { key: 'inGroupNoProduct', label: 'В группах (нет продукта)', icon: IconFolderOpen,  color: 'blue',   path: '/groups?tab=0' },
  { key: 'inGroupNoRights',  label: 'В группах (нет прав)',    icon: IconFolderOpen,   color: 'violet', path: '/groups?tab=1' },
  { key: 'postponedCount',   label: 'Отложено',               icon: IconClock,         color: 'yellow', path: '/postponed' },
  { key: 'backOfficeCount',  label: 'Бэк-офис',               icon: IconBuildingWarehouse, color: 'gray', path: '/groups' },
  { key: 'validatedCount',   label: 'Прошло валидацию',       icon: IconCheck,         color: 'green',  path: '/suspenses' },
  { key: 'totalGroups',      label: 'Групп всего',            icon: IconLayersSubtract, color: 'cyan',  path: '/groups' },
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
      {value === undefined ? (
        <Skeleton height={28} width={60} radius="sm" mt={4} />
      ) : (
        <Text size="xl" fw={700}>{value.toLocaleString('ru-RU')}</Text>
      )}
    </Card>
  );
}

export function DashboardPage() {
  const navigate = useNavigate();
  const pdfExportRef = useRef<typeof import('../utils/dashboardPdfExport') | null>(null);
  const htmlToImageRef = useRef<typeof import('html-to-image') | null>(null);
  const statCardsCaptureRef = useRef<HTMLDivElement>(null);
  const summaryCaptureRef = useRef<HTMLDivElement>(null);
  const donutCaptureRef = useRef<HTMLDivElement>(null);
  const barCaptureRef = useRef<HTMLDivElement>(null);
  const [pdfReady, setPdfReady] = useState(false);
  const [pdfBusy, setPdfBusy] = useState(false);

  useEffect(() => {
    let alive = true;
    void Promise.all([import('../utils/dashboardPdfExport'), import('html-to-image')])
      .then(([pdfMod, hti]) => {
        if (!alive) return;
        pdfExportRef.current = pdfMod;
        htmlToImageRef.current = hti;
        setPdfReady(true);
      })
      .catch((err: unknown) => {
        console.error(err);
        notifications.show({
          color: 'red',
          title: 'PDF',
          message: 'Не удалось подгрузить модуль экспорта. Обновите страницу.',
        });
      });
    return () => {
      alive = false;
    };
  }, []);

  const { data: stats, isLoading, error } = useQuery<DashboardDto>({
    queryKey: ['dashboard'],
    queryFn: () => apiGet<DashboardDto>('/analytics/dashboard'),
    retry: false,
  });

  const donutData = (stats?.statusDistribution ?? [])
    .filter(s => s.count > 0)
    .map(s => ({
      name: STATUS_LABELS[s.status] ?? `Статус ${s.status}`,
      value: s.count,
      color: STATUS_COLORS[s.status] ?? 'gray',
    }));

  const barData = (stats?.topOperators ?? []).map(op => ({
    operator: op.operator.length > 14 ? op.operator.slice(0, 14) + '…' : op.operator,
    count: op.count,
  }));

  return (
    <Stack gap="xl">
      <Group justify="space-between" align="flex-start" wrap="wrap" gap="md">
        <Box>
          <Title order={3} fw={600}>Дашборд</Title>
          <Text c="dimmed" size="sm">Обзор текущего состояния суспенсов</Text>
        </Box>
        <Button
          variant="light"
          color="gray"
          leftSection={<IconFileTypePdf size={18} />}
          disabled={isLoading || !stats || !pdfReady || pdfBusy}
          loading={pdfBusy || (!pdfReady && !isLoading)}
          title={!pdfReady ? 'Подготовка модуля экспорта…' : undefined}
          onClick={() => {
            const mod = pdfExportRef.current;
            const hti = htmlToImageRef.current;
            if (!stats || !mod || !hti) return;
            setPdfBusy(true);
            const snap = async (el: HTMLElement | null) => {
              if (!el) return undefined;
              try {
                return await hti.toPng(el, {
                  cacheBust: true,
                  pixelRatio: 2,
                  backgroundColor: '#ffffff',
                });
              } catch (e) {
                console.warn('Снимок блока для PDF не удался', e);
                return undefined;
              }
            };
            void (async () => {
              try {
                const [statCards, summary, donut, bar] = await Promise.all([
                  snap(statCardsCaptureRef.current),
                  snap(summaryCaptureRef.current),
                  snap(donutCaptureRef.current),
                  snap(barCaptureRef.current),
                ]);
                await mod.downloadDashboardPdf(stats, {
                  statCards,
                  summary,
                  donut,
                  bar,
                });
              } catch (err: unknown) {
                console.error(err);
                notifications.show({
                  color: 'red',
                  title: 'Ошибка PDF',
                  message: err instanceof Error ? err.message : 'Не удалось сформировать файл',
                });
              } finally {
                setPdfBusy(false);
              }
            })();
          }}
        >
          Скачать PDF
        </Button>
      </Group>

      {error && (
        <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">
          Не удалось загрузить данные: {(error as Error).message}
        </Alert>
      )}

      {/* Stat cards — ref для снимка в PDF */}
      <Box ref={statCardsCaptureRef} p="sm" style={{ borderRadius: 8 }}>
        <SimpleGrid cols={{ base: 2, sm: 3, lg: 4 }} spacing="md">
          {STAT_CARDS.map(({ key, label, icon, color, path }) => (
            <StatCard
              key={key}
              label={label}
              value={stats?.[key as keyof DashboardDto] as number | undefined}
              icon={icon}
              color={color}
              onClick={() => navigate(path)}
            />
          ))}
        </SimpleGrid>
      </Box>

      {/* Summary numbers — ref для снимка в PDF */}
      {(isLoading || stats) && (
        <Box ref={summaryCaptureRef} p="sm" style={{ borderRadius: 8 }}>
          <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="md">
            <Paper withBorder radius="md" p="md">
              <Group gap="xs" mb={4}>
                <IconDatabase size={16} color="var(--mantine-color-dimmed)" />
                <Text size="xs" c="dimmed" tt="uppercase" fw={600}>Всего суспенсов</Text>
              </Group>
              {isLoading ? <Skeleton height={28} width={80} /> : (
                <Text size="xl" fw={700}>{stats!.totalSuspenses.toLocaleString('ru-RU')}</Text>
              )}
            </Paper>
            <Paper withBorder radius="md" p="md">
              <Group gap="xs" mb={4}>
                <IconChartBar size={16} color="var(--mantine-color-dimmed)" />
                <Text size="xs" c="dimmed" tt="uppercase" fw={600}>Стримов</Text>
              </Group>
              {isLoading ? <Skeleton height={28} width={80} /> : (
                <Text size="xl" fw={700}>{stats!.totalStreams.toLocaleString('ru-RU')}</Text>
              )}
            </Paper>
            <Paper withBorder radius="md" p="md">
              <Group gap="xs" mb={4}>
                <IconCheck size={16} color="var(--mantine-color-dimmed)" />
                <Text size="xs" c="dimmed" tt="uppercase" fw={600}>Выручка (суспенсы)</Text>
              </Group>
              {isLoading ? <Skeleton height={28} width={80} /> : (
                <Text size="xl" fw={700}>
                  {stats!.totalRevenue.toLocaleString('ru-RU', { maximumFractionDigits: 0 })}
                </Text>
              )}
            </Paper>
          </SimpleGrid>
        </Box>
      )}

      {/* Charts row */}
      <Grid>
        {/* Status distribution donut */}
        <Grid.Col span={{ base: 12, md: 5 }}>
          <Card withBorder radius="md" p="lg" h="100%">
            <Box ref={donutCaptureRef} style={{ borderRadius: 8 }} p="xs">
              <Title order={5} mb="md">Распределение по статусам</Title>
              {isLoading ? (
                <Stack align="center" py="xl" gap="sm">
                  <Skeleton height={200} width={200} circle />
                </Stack>
              ) : donutData.length === 0 ? (
                <Stack align="center" py="xl">
                  <Text c="dimmed" size="sm">Нет данных</Text>
                </Stack>
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
            </Box>
          </Card>
        </Grid.Col>

        {/* Top operators bar chart */}
        <Grid.Col span={{ base: 12, md: 7 }}>
          <Card withBorder radius="md" p="lg" h="100%">
            <Box ref={barCaptureRef} style={{ borderRadius: 8 }} p="xs">
              <Title order={5} mb="md">Топ операторов по количеству суспенсов</Title>
              {isLoading ? (
                <Skeleton height={220} radius="sm" />
              ) : barData.length === 0 ? (
                <Stack align="center" py="xl">
                  <Text c="dimmed" size="sm">Нет данных</Text>
                </Stack>
              ) : (
                <BarChart
                  h={220}
                  data={barData}
                  dataKey="operator"
                  series={[{ name: 'count', label: 'Кол-во', color: 'indigo' }]}
                  tickLine="y"
                  gridAxis="y"
                />
              )}
            </Box>
          </Card>
        </Grid.Col>
      </Grid>

      {/* Quick actions */}
      <Grid>
        <Grid.Col span={{ base: 12, md: 5 }}>
          <Card withBorder radius="md" p="lg">
            <Title order={5} mb="md">Быстрые действия</Title>
            <Stack gap="sm">
              <Button
                variant="light"
                color="indigo"
                leftSection={<IconUpload size={16} />}
                rightSection={<IconArrowRight size={14} />}
                justify="space-between"
                fullWidth
                onClick={() => navigate('/upload')}
              >
                Загрузить отчёт
              </Button>
              <Button
                variant="light"
                color="blue"
                leftSection={<IconLayersSubtract size={16} />}
                rightSection={<IconArrowRight size={14} />}
                justify="space-between"
                fullWidth
                onClick={() => navigate('/grouping')}
              >
                Создать группировку
              </Button>
              <Button
                variant="light"
                color="violet"
                leftSection={<IconFolderOpen size={16} />}
                rightSection={<IconArrowRight size={14} />}
                justify="space-between"
                fullWidth
                onClick={() => navigate('/groups')}
              >
                Сохранённые группы
              </Button>
              <Button
                variant="light"
                color="yellow"
                leftSection={<IconClock size={16} />}
                rightSection={<IconArrowRight size={14} />}
                justify="space-between"
                fullWidth
                onClick={() => navigate('/postponed')}
              >
                Отложенные группы
              </Button>
            </Stack>
          </Card>
        </Grid.Col>

        {/* Catalog summary */}
        <Grid.Col span={{ base: 12, md: 7 }}>
          <Card withBorder radius="md" p="lg" h="100%">
            <Title order={5} mb="md">Справочники</Title>
            <Stack gap="xs">
              <Divider />
              <Group justify="space-between">
                <Text size="sm" c="dimmed">Продуктов в каталоге</Text>
                {isLoading
                  ? <Skeleton height={18} width={40} />
                  : <Text size="sm" fw={600}>{stats!.totalProducts.toLocaleString('ru-RU')}</Text>}
              </Group>
              <Divider />
              <Group justify="space-between">
                <Text size="sm" c="dimmed">Компаний</Text>
                {isLoading
                  ? <Skeleton height={18} width={40} />
                  : <Text size="sm" fw={600}>{stats!.totalCompanies.toLocaleString('ru-RU')}</Text>}
              </Group>
              <Divider />
            </Stack>
          </Card>
        </Grid.Col>
      </Grid>
    </Stack>
  );
}
