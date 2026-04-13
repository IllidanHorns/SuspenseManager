import { useState } from 'react';
import {
  Stack,
  Title,
  Text,
  Group,
  Button,
  Paper,
  Tabs,
  Table,
  ScrollArea,
  Badge,
  Loader,
  Center,
  Alert,
  Box,
  Divider,
  Modal,
  TextInput,
  Textarea,
  NumberInput,
  ActionIcon,
  Tooltip,
  Anchor,
  Pagination,
  SimpleGrid,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { useDisclosure } from '@mantine/hooks';
import { notifications } from '@mantine/notifications';
import {
  IconArrowLeft,
  IconAlertCircle,
  IconCircleCheck,
  IconDownload,
  IconClock,
  IconBuildingWarehouse,
  IconWand,
  IconSearch,
  IconX,
  IconUnlink,
  IconEdit,
  IconArrowBack,
} from '@tabler/icons-react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { getGroupById, getGroupSuspenses } from '../api/groups';
import {
  getMetadata,
  updateMetadata,
  getMetaRights,
  updateMetaRights,
  catalogFast,
  getPossibleProducts,
  linkProduct,
  sendToBackOffice,
  postponeGroup,
  ungroupGroup,
  returnFromPostponed,
  exportGroupSuspenses,
} from '../api/processing';
import { fmtDate, fmtDateTime, downloadBlob } from '../utils/format';
import { StatusBadge } from '../components/common/StatusBadge';
import type { GroupMetadata, GroupMetaRights, CatalogProduct, SuspenseLine } from '../types';

// ─── Metadata Form ────────────────────────────────────────────────────────────

function MetadataTab({ groupId, metadata }: { groupId: number; metadata: GroupMetadata | null }) {
  const qc = useQueryClient();
  const [saving, setSaving] = useState(false);

  const form = useForm({
    initialValues: {
      title: metadata?.title ?? '',
      artist: metadata?.artist ?? '',
      isrc: metadata?.isrc ?? '',
      barcode: metadata?.barcode ?? '',
      catalogNumber: metadata?.catalogNumber ?? '',
      genre: metadata?.genre ?? '',
      description: metadata?.description ?? '',
    },
  });

  const handleSave = async (values: typeof form.values) => {
    setSaving(true);
    try {
      await updateMetadata(groupId, values);
      await qc.invalidateQueries({ queryKey: ['group', groupId] });
      notifications.show({ title: 'Сохранено', message: 'Метаданные обновлены', color: 'green', icon: <IconCircleCheck size={16} /> });
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка сохранения', color: 'red' });
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={form.onSubmit(handleSave)}>
      <Stack gap="md">
        <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
          <TextInput label="Название" {...form.getInputProps('title')} />
          <TextInput label="Исполнитель" {...form.getInputProps('artist')} />
          <TextInput label="ISRC" {...form.getInputProps('isrc')} />
          <TextInput label="Штрих-код" {...form.getInputProps('barcode')} />
          <TextInput label="Кат. номер" {...form.getInputProps('catalogNumber')} />
          <TextInput label="Жанр" {...form.getInputProps('genre')} />
        </SimpleGrid>
        <Textarea label="Описание" {...form.getInputProps('description')} rows={2} />
        <Group justify="flex-end">
          <Button type="submit" loading={saving} color="indigo" leftSection={<IconEdit size={14} />}>
            Сохранить метаданные
          </Button>
        </Group>
      </Stack>
    </form>
  );
}

// ─── Meta-Rights Form ─────────────────────────────────────────────────────────

function MetaRightsTab({ groupId, metaRights }: { groupId: number; metaRights: GroupMetaRights | null }) {
  const qc = useQueryClient();
  const [saving, setSaving] = useState(false);

  const form = useForm({
    initialValues: {
      docNumber: metaRights?.docNumber ?? '',
      docType: metaRights?.docType ?? '',
      docDate: metaRights?.docDate ?? '',
      docStart: metaRights?.docStart ?? '',
      docEnd: metaRights?.docEnd ?? '',
      territoryCode: metaRights?.territoryCode ?? '',
      territoryDesc: metaRights?.territoryDesc ?? '',
      share: metaRights?.share ?? (null as number | null),
    },
  });

  const handleSave = async (values: typeof form.values) => {
    setSaving(true);
    try {
      await updateMetaRights(groupId, values);
      await qc.invalidateQueries({ queryKey: ['group', groupId] });
      notifications.show({ title: 'Сохранено', message: 'Права обновлены', color: 'green', icon: <IconCircleCheck size={16} /> });
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка сохранения', color: 'red' });
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={form.onSubmit(handleSave)}>
      <Stack gap="md">
        <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
          <TextInput label="Номер договора" {...form.getInputProps('docNumber')} />
          <TextInput label="Тип договора" {...form.getInputProps('docType')} />
          <TextInput label="Дата договора" type="date" {...form.getInputProps('docDate')} />
          <TextInput label="Действует с" type="date" {...form.getInputProps('docStart')} />
          <TextInput label="Действует по" type="date" {...form.getInputProps('docEnd')} />
          <TextInput label="Территория (код)" {...form.getInputProps('territoryCode')} />
          <TextInput label="Территория (описание)" {...form.getInputProps('territoryDesc')} />
          <NumberInput label="Доля (%)" min={0} max={100} decimalScale={2} {...form.getInputProps('share')} />
        </SimpleGrid>
        <Group justify="flex-end">
          <Button type="submit" loading={saving} color="violet" leftSection={<IconEdit size={14} />}>
            Сохранить права
          </Button>
        </Group>
      </Stack>
    </form>
  );
}

// ─── Possible Products Modal ──────────────────────────────────────────────────

function PossibleProductsModal({
  opened,
  onClose,
  groupId,
  onLink,
}: {
  opened: boolean;
  onClose: () => void;
  groupId: number;
  onLink: () => void;
}) {
  const [page, setPage] = useState(1);
  const [linking, setLinking] = useState<number | null>(null);
  const { data, isLoading } = useQuery({
    queryKey: ['possible-products', groupId, page],
    queryFn: () => getPossibleProducts(groupId, { pageNumber: page, pageSize: 10 }),
    enabled: opened,
  });

  const handleLink = async (product: CatalogProduct) => {
    setLinking(product.id);
    try {
      await linkProduct(groupId, product.id);
      notifications.show({ title: 'Продукт привязан', message: product.productName ?? `ID: ${product.id}`, color: 'green' });
      onLink();
      onClose();
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка привязки', color: 'red' });
    } finally {
      setLinking(null);
    }
  };

  return (
    <Modal opened={opened} onClose={onClose} title="Возможные продукты" size="lg" radius="md">
      {isLoading ? <Center py="xl"><Loader color="indigo" /></Center> : (
        <Stack gap="md">
          <Table striped highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>ID</Table.Th>
                <Table.Th>Название</Table.Th>
                <Table.Th>Исполнитель</Table.Th>
                <Table.Th>ISRC</Table.Th>
                <Table.Th></Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {(data?.items ?? []).map((p) => (
                <Table.Tr key={p.id}>
                  <Table.Td>{p.id}</Table.Td>
                  <Table.Td>{p.productName ?? '—'}</Table.Td>
                  <Table.Td>{p.artist ?? '—'}</Table.Td>
                  <Table.Td>{p.isrc ?? '—'}</Table.Td>
                  <Table.Td>
                    <Button
                      size="xs"
                      color="indigo"
                      variant="light"
                      loading={linking === p.id}
                      onClick={() => handleLink(p)}
                    >
                      Привязать
                    </Button>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
          <Group justify="space-between" pt="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {data?.totalCount ?? 0}</Text>
            <Pagination value={page} onChange={setPage} total={Math.max(1, data?.totalPages ?? 1)} size="sm" />
          </Group>
        </Stack>
      )}
    </Modal>
  );
}

// ─── Suspenses Tab ────────────────────────────────────────────────────────────

function SuspensesTab({ groupId }: { groupId: number }) {
  const [page, setPage] = useState(1);
  const [pendingIsrc, setPendingIsrc] = useState('');
  const [pendingArtist, setPendingArtist] = useState('');
  const [pendingOperator, setPendingOperator] = useState('');
  const [applied, setApplied] = useState<Record<string, string>>({});
  const hasActive = Object.keys(applied).length > 0;

  const applyFilters = () => {
    const f: Record<string, string> = {};
    if (pendingIsrc.trim())     f['Isrc_contains']    = pendingIsrc.trim();
    if (pendingArtist.trim())   f['Artist_contains']  = pendingArtist.trim();
    if (pendingOperator.trim()) f['Operator_contains'] = pendingOperator.trim();
    setApplied(f);
    setPage(1);
  };

  const resetFilters = () => {
    setPendingIsrc(''); setPendingArtist(''); setPendingOperator('');
    setApplied({});
    setPage(1);
  };

  const onKey = (e: React.KeyboardEvent) => { if (e.key === 'Enter') applyFilters(); };

  const { data, isLoading } = useQuery({
    queryKey: ['group-suspenses', groupId, page, applied],
    queryFn: () => getGroupSuspenses(groupId, { pageNumber: page, pageSize: 20, Filters: applied }),
  });

  const rows = data?.items ?? [];

  return (
    <Stack gap="md">
      <Paper withBorder radius="md" p="sm">
        <Group gap="sm" wrap="wrap" align="flex-end">
          <TextInput size="xs" label="ISRC" placeholder="ISRC" value={pendingIsrc}
            onChange={(e) => setPendingIsrc(e.target.value)} onKeyDown={onKey} style={{ width: 160 }} />
          <TextInput size="xs" label="Исполнитель" placeholder="Исполнитель" value={pendingArtist}
            onChange={(e) => setPendingArtist(e.target.value)} onKeyDown={onKey} style={{ flex: 1, minWidth: 140 }} />
          <TextInput size="xs" label="Оператор" placeholder="Оператор" value={pendingOperator}
            onChange={(e) => setPendingOperator(e.target.value)} onKeyDown={onKey} style={{ flex: 1, minWidth: 120 }} />
          <Group gap="xs">
            <Button size="xs" leftSection={<IconSearch size={12} />} onClick={applyFilters}>Найти</Button>
            {hasActive && (
              <Button size="xs" variant="subtle" color="gray" leftSection={<IconX size={12} />} onClick={resetFilters}>
                Сбросить
              </Button>
            )}
          </Group>
        </Group>
      </Paper>
      {isLoading ? <Center py="xl"><Loader color="indigo" /></Center> : (
        <>
          <ScrollArea>
            <Table striped highlightOnHover style={{ minWidth: 900 }}>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>ID</Table.Th>
                  <Table.Th>ISRC</Table.Th>
                  <Table.Th>Исполнитель</Table.Th>
                  <Table.Th>Трек</Table.Th>
                  <Table.Th>Оператор</Table.Th>
                  <Table.Th>Территория</Table.Th>
                  <Table.Th>Qty</Table.Th>
                  <Table.Th>PPD</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {rows.map((s: SuspenseLine) => (
                  <Table.Tr key={s.id}>
                    <Table.Td><Text size="sm" fw={600}>{s.id}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.isrc ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.artist ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.trackTitle ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.operator ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.territoryCode ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.qty}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.ppd}</Text></Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
          <Group justify="space-between" px="sm" pt="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {data?.totalCount ?? 0}</Text>
            <Pagination value={page} onChange={setPage} total={Math.max(1, data?.totalPages ?? 1)} size="sm" />
          </Group>
        </>
      )}
    </Stack>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export function GroupDetailPage() {
  const { id } = useParams<{ id: string }>();
  const groupId = Number(id);
  const navigate = useNavigate();
  const qc = useQueryClient();

  const { data: group, isLoading, error } = useQuery({
    queryKey: ['group', groupId],
    queryFn: () => getGroupById(groupId),
  });

  const { data: metadata } = useQuery({
    queryKey: ['metadata', groupId],
    queryFn: () => getMetadata(groupId),
    enabled: !!group,
  });

  const { data: metaRights } = useQuery({
    queryKey: ['meta-rights', groupId],
    queryFn: () => getMetaRights(groupId),
    enabled: !!group,
  });

  const [possibleOpen, { open: openPossible, close: closePossible }] = useDisclosure(false);
  const [boOpen, { open: openBo, close: closeBo }] = useDisclosure(false);
  const [postponeOpen, { open: openPostpone, close: closePostpone }] = useDisclosure(false);
  const [ungroupOpen, { open: openUngroup, close: closeUngroup }] = useDisclosure(false);

  const [boComment, setBoComment] = useState('');
  const [postponeReason, setPostponeReason] = useState('');
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  const runAction = async (key: string, fn: () => Promise<unknown>) => {
    setActionLoading(key);
    try {
      await fn();
      await qc.invalidateQueries({ queryKey: ['group', groupId] });
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setActionLoading(null);
    }
  };

  const handleCatalogFast = () =>
    runAction('catalog-fast', async () => {
      await catalogFast(groupId);
      notifications.show({ title: 'Продукт создан', message: 'Быстрая каталогизация выполнена', color: 'green' });
    });

  const handleSendBo = async () => {
    await runAction('bo', async () => {
      await sendToBackOffice(groupId, boComment);
      notifications.show({ title: 'Отправлено в бэк-офис', message: '', color: 'blue' });
      closeBo();
    });
  };

  const handlePostpone = async () => {
    await runAction('postpone', async () => {
      await postponeGroup(groupId, postponeReason);
      notifications.show({ title: 'Группа отложена', message: '', color: 'yellow' });
      closePostpone();
      navigate('/postponed');
    });
  };

  const handleUngroup = async () => {
    await runAction('ungroup', async () => {
      await ungroupGroup(groupId);
      notifications.show({ title: 'Группа расформирована', message: '', color: 'gray' });
      closeUngroup();
      navigate('/groups');
    });
  };

  const handleReturn = () =>
    runAction('return', async () => {
      await returnFromPostponed(groupId);
      notifications.show({ title: 'Группа возвращена в обработку', message: '', color: 'green' });
      navigate('/groups');
    });

  const handleExport = async () => {
    try {
      const blob = await exportGroupSuspenses(groupId);
      downloadBlob(blob, `group_${groupId}_suspenses.xlsx`);
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка экспорта', color: 'red' });
    }
  };

  if (isLoading) return <Center py="xl"><Loader color="indigo" /></Center>;
  if (error || !group) return (
    <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">
      {error?.message ?? 'Группа не найдена'}
    </Alert>
  );

  const isNoProduct = group.businessStatus === 15;
  const isNoRights = group.businessStatus === 16;
  const isPostponed = group.businessStatus === 30 || group.businessStatus === 32;
  const canUngroup = [15, 16, 30, 32].includes(group.businessStatus);

  return (
    <Stack gap="xl">
      {/* Header */}
      <Box>
        <Group mb="xs">
          <Anchor component="button" onClick={() => navigate('/groups')} size="sm" c="dimmed">
            <Group gap={4}><IconArrowLeft size={14} />Группы</Group>
          </Anchor>
        </Group>
        <Group justify="space-between" align="flex-start" wrap="wrap" gap="md">
          <Box>
            <Group gap="sm" mb={4}>
              <Title order={3} fw={600}>Группа #{group.id}</Title>
              <StatusBadge status={group.businessStatus} />
            </Group>
            <Text size="sm" c="dimmed">
              Создана: {fmtDateTime(group.createTime)}
              {group.changeTime && ` · Изменена: ${fmtDateTime(group.changeTime)}`}
            </Text>
          </Box>

          {/* Action buttons */}
          <Group gap="xs" wrap="wrap">
            {isNoProduct && (
              <>
                <Tooltip label="Быстрая каталогизация — создать новый продукт из метаданных">
                  <Button
                    size="sm"
                    color="teal"
                    variant="light"
                    leftSection={<IconWand size={14} />}
                    loading={actionLoading === 'catalog-fast'}
                    onClick={handleCatalogFast}
                  >
                    Быстрый каталог
                  </Button>
                </Tooltip>
                <Button
                  size="sm"
                  color="blue"
                  variant="light"
                  leftSection={<IconSearch size={14} />}
                  onClick={openPossible}
                >
                  Возможные продукты
                </Button>
              </>
            )}
            <Button
              size="sm"
              color="green"
              variant="light"
              leftSection={<IconDownload size={14} />}
              onClick={handleExport}
            >
              Экспорт
            </Button>
            {/* Отложить — только для активных групп 15/16 */}
            {(isNoProduct || isNoRights) && (
              <Button
                size="sm"
                color="gray"
                variant="light"
                leftSection={<IconBuildingWarehouse size={14} />}
                onClick={openBo}
              >
                Бэк-офис
              </Button>
            )}
            {(isNoProduct || isNoRights) && (
              <Button
                size="sm"
                color="yellow"
                variant="light"
                leftSection={<IconClock size={14} />}
                onClick={openPostpone}
              >
                Отложить
              </Button>
            )}
            {/* Вернуть из отложенных — только для 30/32 */}
            {isPostponed && (
              <Button
                size="sm"
                color="green"
                variant="light"
                leftSection={<IconArrowBack size={14} />}
                loading={actionLoading === 'return'}
                onClick={handleReturn}
              >
                Вернуть в обработку
              </Button>
            )}
            {/* Расформировать — для 15/16/30/32 */}
            {canUngroup && (
              <Button
                size="sm"
                color="red"
                variant="light"
                leftSection={<IconUnlink size={14} />}
                onClick={openUngroup}
              >
                Расформировать
              </Button>
            )}
          </Group>
        </Group>
      </Box>

      <Divider />

      {/* Tabs */}
      <Tabs defaultValue="suspenses" variant="outline" radius="md">
        <Tabs.List>
          <Tabs.Tab value="suspenses">Строки суспенсов</Tabs.Tab>
          <Tabs.Tab value="metadata">Метаданные продукта</Tabs.Tab>
          {isNoRights && <Tabs.Tab value="rights">Метаданные прав</Tabs.Tab>}
        </Tabs.List>

        <Tabs.Panel value="suspenses" pt="md">
          <SuspensesTab groupId={groupId} />
        </Tabs.Panel>

        <Tabs.Panel value="metadata" pt="md">
          <Paper withBorder radius="md" p="md">
            <MetadataTab groupId={groupId} metadata={metadata ?? null} />
          </Paper>
        </Tabs.Panel>

        {isNoRights && (
          <Tabs.Panel value="rights" pt="md">
            <Paper withBorder radius="md" p="md">
              <MetaRightsTab groupId={groupId} metaRights={metaRights ?? null} />
            </Paper>
          </Tabs.Panel>
        )}
      </Tabs>

      {/* Possible Products Modal */}
      <PossibleProductsModal
        opened={possibleOpen}
        onClose={closePossible}
        groupId={groupId}
        onLink={() => qc.invalidateQueries({ queryKey: ['group', groupId] })}
      />

      {/* Back Office Modal */}
      <Modal opened={boOpen} onClose={closeBo} title="Отправить в бэк-офис" centered radius="md">
        <Stack gap="md">
          <Textarea
            label="Комментарий"
            placeholder="Опишите причину отправки..."
            value={boComment}
            onChange={(e) => setBoComment(e.target.value)}
            rows={3}
          />
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={closeBo}>Отмена</Button>
            <Button color="gray" loading={actionLoading === 'bo'} onClick={handleSendBo}>
              Отправить
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* Postpone Modal */}
      <Modal opened={postponeOpen} onClose={closePostpone} title="Отложить группу" centered radius="md">
        <Stack gap="md">
          <Textarea
            label="Причина"
            placeholder="Укажите причину..."
            value={postponeReason}
            onChange={(e) => setPostponeReason(e.target.value)}
            rows={3}
          />
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={closePostpone}>Отмена</Button>
            <Button color="yellow" loading={actionLoading === 'postpone'} onClick={handlePostpone}>
              Отложить
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* Ungroup Modal */}
      <Modal opened={ungroupOpen} onClose={closeUngroup} title="Расформировать группу" centered radius="md">
        <Stack gap="md">
          <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">
            Группа будет архивирована, все суспенсы вернутся в исходный статус. Это действие нельзя отменить.
          </Alert>
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={closeUngroup}>Отмена</Button>
            <Button color="red" loading={actionLoading === 'ungroup'} onClick={handleUngroup}>
              Расформировать
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}
