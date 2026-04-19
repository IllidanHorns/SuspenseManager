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
  ActionIcon,
  Tooltip,
  Modal,
  Button,
  TextInput,
  NumberInput,
  Switch,
} from '@mantine/core';
import {
  IconClock,
  IconAlertCircle,
  IconArrowBack,
  IconRefresh,
  IconEye,
  IconUnlink,
  IconSearch,
  IconX,
} from '@tabler/icons-react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useDisclosure } from '@mantine/hooks';
import { getPostponedGroups, type GroupListRequest } from '../api/groups';
import { returnFromPostponed, ungroupGroup } from '../api/processing';
import { fmtDateTime } from '../utils/format';
import { StatusBadge } from '../components/common/StatusBadge';
import { PageSizeSelect } from '../components/common/PageSizeSelect';
import { ResizableTh } from '../components/common/ResizableTh';
import { CollapsibleFilters } from '../components/common/CollapsibleFilters';
import { notifications } from '@mantine/notifications';
import { useDefaultPageSize } from '../hooks/useDefaultPageSize';

function val(v: string | null | undefined) {
  return v ?? '—';
}

function fmtRevenue(v: number | undefined) {
  if (v === undefined || v === null) return '—';
  return v.toLocaleString('ru-RU', { maximumFractionDigits: 2 });
}

interface FilterState {
  artist: string;
  title: string;
  isrc: string;
  countMin: number | '';
  countMax: number | '';
  revenueMin: number | '';
  revenueMax: number | '';
}

const EMPTY_FILTER: FilterState = {
  artist: '', title: '', isrc: '',
  countMin: '', countMax: '', revenueMin: '', revenueMax: '',
};

function filterToRequest(f: FilterState): Partial<GroupListRequest> {
  const r: Partial<GroupListRequest> = {};
  if (f.artist) r.artist = f.artist;
  if (f.title) r.title = f.title;
  if (f.isrc) r.isrc = f.isrc;
  if (f.countMin !== '') r.countMin = Number(f.countMin);
  if (f.countMax !== '') r.countMax = Number(f.countMax);
  if (f.revenueMin !== '') r.revenueMin = Number(f.revenueMin);
  if (f.revenueMax !== '') r.revenueMax = Number(f.revenueMax);
  return r;
}

export function PostponedPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useDefaultPageSize();
  const [returning, setReturning] = useState<number | null>(null);
  const [ungroupTarget, setUngroupTarget] = useState<number | null>(null);
  const [ungroupLoading, setUngroupLoading] = useState(false);
  const [confirmOpened, { open: openConfirm, close: closeConfirm }] = useDisclosure(false);
  const navigate = useNavigate();
  const qc = useQueryClient();

  const [pending, setPending] = useState<FilterState>(EMPTY_FILTER);
  const [applied, setApplied] = useState<Partial<GroupListRequest>>({});
  const [onlyMine, setOnlyMine] = useState(false);

  const handlePageSizeChange = (v: number) => { setPageSize(v); setPage(1); };

  const applyFilters = () => { setApplied(filterToRequest(pending)); setPage(1); };
  const resetFilters = () => {
    setPending(EMPTY_FILTER);
    setApplied({});
    setOnlyMine(false);
    setPage(1);
  };

  const hasActive = Object.values(pending).some((v) => v !== '' && v !== null);

  const set = (field: keyof FilterState) => (val: string | number) =>
    setPending((p) => ({ ...p, [field]: val }));

  const handleEnter = (e: React.KeyboardEvent) => { if (e.key === 'Enter') applyFilters(); };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['postponed', page, pageSize, applied, onlyMine],
    queryFn: () =>
      getPostponedGroups({
        pageNumber: page,
        pageSize,
        ...applied,
        ...(onlyMine ? { onlyMine: true } : {}),
      }),
  });

  const handleReturn = async (groupId: number) => {
    setReturning(groupId);
    try {
      await returnFromPostponed(groupId);
      notifications.show({ title: 'Группа возвращена', message: `Группа #${groupId} возвращена в обработку`, color: 'green' });
      await qc.invalidateQueries({ queryKey: ['postponed'] });
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setReturning(null);
    }
  };

  const openUngroupConfirm = (groupId: number) => { setUngroupTarget(groupId); openConfirm(); };

  const handleUngroup = async () => {
    if (!ungroupTarget) return;
    setUngroupLoading(true);
    try {
      await ungroupGroup(ungroupTarget);
      notifications.show({ title: 'Группа расформирована', message: `Группа #${ungroupTarget} расформирована`, color: 'gray' });
      closeConfirm();
      await qc.invalidateQueries({ queryKey: ['postponed'] });
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setUngroupLoading(false);
    }
  };

  return (
    <Stack gap="xl">
      <Box>
        <Title order={3} fw={600}>Отложенные группы</Title>
        <Text c="dimmed" size="sm">Группы, отложенные на более поздний срок</Text>
      </Box>

      <Group justify="space-between" wrap="wrap">
        <Switch
          label="Только мои"
          checked={onlyMine}
          onChange={(e) => {
            setOnlyMine(e.currentTarget.checked);
            setPage(1);
          }}
          size="sm"
        />
        <Tooltip label="Обновить">
          <ActionIcon variant="light" color="gray" onClick={() => refetch()}>
            <IconRefresh size={16} />
          </ActionIcon>
        </Tooltip>
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
                {hasActive && (
                  <Button size="xs" variant="subtle" color="gray" leftSection={<IconX size={12} />} onClick={resetFilters}>
                    Сбросить
                  </Button>
                )}
              </Group>
            </Group>
          </Stack>
        </Paper>
      </CollapsibleFilters>

      {isLoading ? (
        <Center py="xl"><Loader color="indigo" /></Center>
      ) : error ? (
        <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">{error.message}</Alert>
      ) : !data?.items.length ? (
        <Center py="xl">
          <Stack align="center" gap="xs">
            <IconClock size={40} color="var(--mantine-color-dimmed)" />
            <Text c="dimmed">Нет отложенных групп</Text>
          </Stack>
        </Center>
      ) : (
        <Paper withBorder radius="md">
          <ScrollArea>
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <ResizableTh>ID</ResizableTh>
                  <ResizableTh>Статус</ResizableTh>
                  <ResizableTh>Строк</ResizableTh>
                  <ResizableTh>Выручка, ₽</ResizableTh>
                  <ResizableTh>Исполнитель</ResizableTh>
                  <ResizableTh>Трек / Продукт</ResizableTh>
                  <ResizableTh>ISRC</ResizableTh>
                  <ResizableTh>Отложена</ResizableTh>
                  <ResizableTh style={{ textAlign: 'center' }}>Действия</ResizableTh>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {data.items.map((g) => {
                  const artist = g.groupMetaData?.artist ?? g.catalogProduct?.artist;
                  const title = g.groupMetaData?.title ?? g.catalogProduct?.productName;
                  const isrc = g.groupMetaData?.isrc ?? g.catalogProduct?.isrc;
                  return (
                    <Table.Tr key={g.id}>
                      <Table.Td><Text size="sm" fw={600} c="indigo">#{g.id}</Text></Table.Td>
                      <Table.Td><StatusBadge status={g.businessStatus} /></Table.Td>
                      <Table.Td>
                        <Badge variant="light" color="blue" size="sm">{g.suspenseCount ?? '—'}</Badge>
                      </Table.Td>
                      <Table.Td><Text size="sm">{fmtRevenue(g.revenueRub)}</Text></Table.Td>
                      <Table.Td><Text size="sm">{val(artist)}</Text></Table.Td>
                      <Table.Td><Text size="sm">{val(title)}</Text></Table.Td>
                      <Table.Td><Text size="xs" ff="monospace">{val(isrc)}</Text></Table.Td>
                      <Table.Td>
                        <Text size="sm" c="dimmed">{fmtDateTime(g.changeTime ?? g.createTime)}</Text>
                      </Table.Td>
                      <Table.Td>
                        <Group justify="center" gap="xs">
                          <Tooltip label="Открыть">
                            <ActionIcon variant="light" color="indigo" size="sm" onClick={() => navigate(`/groups/${g.id}`)}>
                              <IconEye size={14} />
                            </ActionIcon>
                          </Tooltip>
                          <Tooltip label="Вернуть в обработку">
                            <ActionIcon variant="light" color="green" size="sm"
                              loading={returning === g.id} onClick={() => handleReturn(g.id)}>
                              <IconArrowBack size={14} />
                            </ActionIcon>
                          </Tooltip>
                          <Tooltip label="Расформировать группу">
                            <ActionIcon variant="light" color="red" size="sm"
                              onClick={() => openUngroupConfirm(g.id)}>
                              <IconUnlink size={14} />
                            </ActionIcon>
                          </Tooltip>
                        </Group>
                      </Table.Td>
                    </Table.Tr>
                  );
                })}
              </Table.Tbody>
            </Table>
          </ScrollArea>
          <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {data.totalCount.toLocaleString('ru-RU')}</Text>
            <Group gap="sm">
              <PageSizeSelect value={pageSize} onChange={handlePageSizeChange} />
              <Pagination value={page} onChange={setPage} total={Math.max(1, data.totalPages)} size="sm" />
            </Group>
          </Group>
        </Paper>
      )}

      <Modal opened={confirmOpened} onClose={closeConfirm} title="Расформировать группу" centered radius="md">
        <Stack gap="md">
          <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">
            Группа #{ungroupTarget} будет архивирована. Все суспенсы вернутся в исходный статус. Это действие нельзя отменить.
          </Alert>
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={closeConfirm}>Отмена</Button>
            <Button color="red" loading={ungroupLoading} onClick={handleUngroup}>Расформировать</Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}
