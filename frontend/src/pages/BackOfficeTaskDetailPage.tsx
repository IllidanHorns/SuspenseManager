import { useState } from 'react';
import {
  Stack, Box, Title, Text, Paper, Group, Button, Badge, Tabs,
  Table, ScrollArea, Pagination, Modal, Textarea, TextInput,
  SimpleGrid, NumberInput, Select, Alert, Center, Loader,
  Anchor, Breadcrumbs, Tooltip,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import {
  IconArrowLeft, IconAlertCircle, IconCheck, IconSearch,
  IconLink, IconArrowBack, IconTrash, IconInfoCircle, IconFileInfo,
} from '@tabler/icons-react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import {
  getBoTask, boReturnGroup, boDeleteGroup, boLinkProduct, boCompleteTask,
  boGetPossibleProducts, boSearchRights, boCopyRights,
  boUpdateMetadata, boUpdateMetaRights,
} from '../api/backoffice';
import { SearchRightsCatalogModal } from '../components/groups/SearchRightsCatalogModal';
import { ProductDetailModal } from '../components/groups/ProductDetailModal';
import { getGroupSuspenses } from '../api/groups';
import { getCompanies } from '../api/companies';
import { getTerritories } from '../api/territories';
import { PageSizeSelect } from '../components/common/PageSizeSelect';
import { ResizableTh } from '../components/common/ResizableTh';
import { StatusBadge } from '../components/common/StatusBadge';
import { STATUS_LABELS } from '../types';
import { fmtDateTime } from '../utils/format';
import { usePermissions } from '../hooks/usePermissions';
import { PermissionCodes } from '../utils/permissions';
import { DbMax, combine, dateIsoOptional, optMaxLen, required, sharePercent } from '../utils/fieldValidation';
import type {
  CatalogProduct,
  UpdateGroupMetadataDto, UpdateGroupMetaRightsDto,
} from '../types';

export function BackOfficeTaskDetailPage() {
  const { taskId } = useParams<{ taskId: string }>();
  const tid = Number(taskId);
  const navigate = useNavigate();
  const qc = useQueryClient();

  const { data: task, isLoading, error } = useQuery({
    queryKey: ['bo-task', tid],
    queryFn: () => getBoTask(tid),
    enabled: !isNaN(tid),
  });

  const group = task?.group;
  const groupId = task?.groupId;
  const status = group?.businessStatus;
  const is120 = status === 120;
  const is320 = status === 320;

  const { hasPermission } = usePermissions();
  const permLinkProduct = hasPermission(PermissionCodes.backofficeLinkProduct);
  const permCopyRights = hasPermission(PermissionCodes.backofficeCopyRights);
  const permValidate = hasPermission(PermissionCodes.backofficeValidate);
  const permReturn = hasPermission(PermissionCodes.backofficeReturn);
  const permDelete = hasPermission(PermissionCodes.backofficeDelete);

  const refresh = () => qc.invalidateQueries({ queryKey: ['bo-task', tid] });
  const [searchRightsOpen, { open: openSearchRights, close: closeSearchRights }] = useDisclosure(false);

  if (isLoading) return <Center py="xl"><Loader /></Center>;
  if (error || !task) return (
    <Alert icon={<IconAlertCircle size={16} />} color="red">
      {error instanceof Error ? error.message : 'Задание не найдено'}
    </Alert>
  );

  return (
    <Stack gap="xl">
      {/* Breadcrumb */}
      <Breadcrumbs>
        <Anchor size="sm" onClick={() => navigate('/backoffice/tasks')}>← Задания БО</Anchor>
        <Text size="sm">Задание #{tid}</Text>
      </Breadcrumbs>

      {/* Header */}
      <Box>
        <Group gap="sm" mb={4}>
          <Title order={3} fw={600}>Задание #{tid}</Title>
          <StatusBadge status={status!} />
        </Group>
        <Text size="sm" c="dimmed">Группа #{groupId} · Суспенсов: {group?.suspenseCount ?? '—'}</Text>
      </Box>

      {/* Problem description */}
      <Paper withBorder radius="md" p="md">
        <Text size="xs" c="dimmed" fw={500} mb={4}>ОПИСАНИЕ ПРОБЛЕМЫ ОТ ОПЕРАТОРА</Text>
        <Text size="sm" style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', maxHeight: 200, overflowY: 'auto' }}>
          {task.problemDescription}
        </Text>
        <Text size="xs" c="dimmed" mt="xs">
          Создано: {fmtDateTime(task.createTime)}
        </Text>
      </Paper>

      {/* Action buttons */}
      <Group gap="sm">
        {is120 && permLinkProduct && (
          <PossibleProductsButton taskId={tid} groupId={groupId!} onLinked={refresh} />
        )}
        {is320 && (
          <>
            {permValidate && (
              <CompleteTaskButton taskId={tid} group={group!} onDone={() => navigate('/backoffice/tasks')} />
            )}
            {permCopyRights && (
              <>
                <Button
                  leftSection={<IconSearch size={14} />}
                  variant="light"
                  color="violet"
                  onClick={openSearchRights}
                  size="sm"
                >
                  Найти права
                </Button>
                <SearchRightsCatalogModal
                  opened={searchRightsOpen}
                  onClose={closeSearchRights}
                  group={group!}
                  onApplied={() => {
                    qc.invalidateQueries({ queryKey: ['bo-tasks'] });
                    refresh();
                    navigate('/backoffice/tasks');
                  }}
                  searchRights={(p) => boSearchRights(tid, p)}
                  copyRights={async (rightsId) => {
                    await boCopyRights(tid, rightsId);
                  }}
                />
              </>
            )}
          </>
        )}
        {permReturn && <ReturnButton taskId={tid} onDone={() => navigate('/backoffice/tasks')} />}
        {permDelete && <DeleteButton taskId={tid} onDone={() => navigate('/backoffice/tasks')} />}
      </Group>

      {/* Tabs */}
      <Tabs defaultValue="suspenses" variant="outline" radius="md">
        <Tabs.List>
          <Tabs.Tab value="suspenses">Суспенсы</Tabs.Tab>
          <Tabs.Tab value="metadata">Метаданные</Tabs.Tab>
          {is320 && <Tabs.Tab value="meta-rights">Метаправа</Tabs.Tab>}
        </Tabs.List>

        <Tabs.Panel value="suspenses" pt="md">
          <SuspensesTab groupId={groupId!} />
        </Tabs.Panel>

        <Tabs.Panel value="metadata" pt="md">
          <MetadataTab taskId={tid} groupId={groupId!} group={group!} onSaved={refresh} readOnly={is320 || !permLinkProduct} />
        </Tabs.Panel>

        {is320 && (
          <Tabs.Panel value="meta-rights" pt="md">
            <MetaRightsTab taskId={tid} groupId={groupId!} group={group!} onSaved={refresh} />
          </Tabs.Panel>
        )}
      </Tabs>
    </Stack>
  );
}

// ── Suspenses tab ─────────────────────────────────────────────────────────────

function SuspensesTab({ groupId }: { groupId: number }) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const { data, isLoading } = useQuery({
    queryKey: ['group-suspenses', groupId, page, pageSize],
    queryFn: () => getGroupSuspenses(groupId, { pageNumber: page, pageSize }),
  });

  if (isLoading) return <Center py="xl"><Loader size="sm" /></Center>;
  if (!data?.items.length) return <Center py="xl"><Text c="dimmed">Суспенсов нет</Text></Center>;

  return (
    <Paper withBorder radius="md">
      <ScrollArea>
        <Table striped style={{ minWidth: 800 }}>
          <Table.Thead>
            <Table.Tr>
              <ResizableTh>ID</ResizableTh>
              <ResizableTh>ISRC</ResizableTh>
              <ResizableTh>Артист</ResizableTh>
              <ResizableTh>Название</ResizableTh>
              <ResizableTh>Оператор</ResizableTh>
              <ResizableTh>Территория</ResizableTh>
              <ResizableTh>Кол-во</ResizableTh>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {data.items.map((s) => (
              <Table.Tr key={s.id}>
                <Table.Td><Text size="sm">{s.id}</Text></Table.Td>
                <Table.Td><Text size="sm">{s.isrc ?? '—'}</Text></Table.Td>
                <Table.Td><Text size="sm">{s.artist ?? '—'}</Text></Table.Td>
                <Table.Td><Text size="sm">{s.trackTitle ?? '—'}</Text></Table.Td>
                <Table.Td><Text size="sm">{s.operator ?? '—'}</Text></Table.Td>
                <Table.Td><Text size="sm">{s.territoryCode ?? '—'}</Text></Table.Td>
                <Table.Td><Text size="sm">{s.qty}</Text></Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </ScrollArea>
      <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
        <Text size="sm" c="dimmed">Всего: {data.totalCount}</Text>
        <Group gap="sm">
          <PageSizeSelect value={pageSize} onChange={(v) => { setPageSize(v); setPage(1); }} />
          <Pagination value={page} onChange={setPage} total={Math.max(1, data.totalPages)} size="sm" />
        </Group>
      </Group>
    </Paper>
  );
}

// ── Metadata tab ──────────────────────────────────────────────────────────────

function MetadataTab({ taskId, groupId, group, onSaved, readOnly }: {
  taskId: number; groupId: number; group: any; onSaved: () => void; readOnly: boolean;
}) {
  const [saving, setSaving] = useState(false);
  const qc = useQueryClient();
  const meta = group?.groupMetaData;
  const product = group?.catalogProduct;

  const form = useForm<UpdateGroupMetadataDto>({
    initialValues: {
      title: meta?.title ?? product?.productName ?? '',
      artist: meta?.artist ?? product?.artist ?? '',
      isrc: meta?.isrc ?? product?.isrc ?? '',
      barcode: meta?.barcode ?? product?.barcode ?? '',
      catalogNumber: meta?.catalogNumber ?? '',
      genre: meta?.genre ?? '',
      description: meta?.description ?? '',
    },
    validate: {
      title: optMaxLen(DbMax.groupMetadata.title, 'Название'),
      artist: optMaxLen(DbMax.groupMetadata.artist, 'Исполнитель'),
      isrc: optMaxLen(DbMax.groupMetadata.isrc, 'ISRC'),
      barcode: optMaxLen(DbMax.groupMetadata.barcode, 'Баркод'),
      catalogNumber: optMaxLen(DbMax.groupMetadata.catalogNumber, 'Каталожный номер'),
      genre: optMaxLen(DbMax.groupMetadata.genre, 'Жанр'),
      description: optMaxLen(DbMax.groupMetadata.description, 'Описание'),
    },
  });

  const handleSave = async (values: UpdateGroupMetadataDto) => {
    setSaving(true);
    try {
      await boUpdateMetadata(taskId, groupId, values);
      onSaved();
      qc.invalidateQueries({ queryKey: ['group-suspenses', groupId] });
      notifications.show({ title: 'Сохранено', message: 'Метаданные обновлены', color: 'green' });
    } catch (e) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setSaving(false);
    }
  };

  const display = (v: string | null | undefined) => (v?.trim() ? v : '—');

  if (readOnly) {
    return (
      <Paper withBorder radius="md" p="md">
        <Alert color="blue" mb="md" radius="md" icon={<IconAlertCircle size={16} />}>
          Продукт каталога привязан: <strong>#{product?.id}</strong>{product?.productName ? ` — ${product.productName}` : ''}.
          Данные продукта берутся из каталога и недоступны для редактирования здесь.
        </Alert>
        <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
          {[
            { label: 'Название', value: meta?.title ?? product?.productName },
            { label: 'Артист', value: meta?.artist ?? product?.artist },
            { label: 'ISRC', value: meta?.isrc ?? product?.isrc },
            { label: 'Баркод', value: meta?.barcode ?? product?.barcode },
            { label: 'Каталожный номер', value: meta?.catalogNumber ?? product?.catalogNumber },
            { label: 'Жанр', value: meta?.genre ?? product?.genre },
          ].map(({ label, value }) => (
            <Box key={label}>
              <Text size="xs" c="dimmed">{label}</Text>
              <Text size="sm">{display(value)}</Text>
            </Box>
          ))}
        </SimpleGrid>
        {(meta?.description) && (
          <Box mt="sm">
            <Text size="xs" c="dimmed">Описание</Text>
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>{meta.description}</Text>
          </Box>
        )}
      </Paper>
    );
  }

  return (
    <Paper withBorder radius="md" p="md">
      {product && (
        <Alert color="blue" mb="md" radius="md">
          Продукт каталога привязан: <strong>#{product.id}</strong>{product.productName ? ` — ${product.productName}` : ''}
        </Alert>
      )}
      <form onSubmit={form.onSubmit(handleSave)}>
        <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
          <TextInput label="Название" {...form.getInputProps('title')} />
          <TextInput label="Артист" {...form.getInputProps('artist')} />
          <TextInput label="ISRC" {...form.getInputProps('isrc')} />
          <TextInput label="Баркод" {...form.getInputProps('barcode')} />
          <TextInput label="Каталожный номер" {...form.getInputProps('catalogNumber')} />
          <TextInput label="Жанр" {...form.getInputProps('genre')} />
        </SimpleGrid>
        <Textarea label="Описание" mt="sm" {...form.getInputProps('description')} />
        <Group justify="flex-end" mt="md">
          <Button type="submit" loading={saving} size="sm">Сохранить</Button>
        </Group>
      </form>
    </Paper>
  );
}

// ── Meta-rights tab ───────────────────────────────────────────────────────────

function MetaRightsTab({ taskId, groupId, group, onSaved }: {
  taskId: number; groupId: number; group: any; onSaved: () => void;
}) {
  const [saving, setSaving] = useState(false);
  const qc = useQueryClient();
  const rights = group?.groupMetaRights;

  const { data: companiesData } = useQuery({
    queryKey: ['companies'],
    queryFn: () => getCompanies(),
    staleTime: 5 * 60 * 1000,
  });

  const { data: territoriesData } = useQuery({
    queryKey: ['territories'],
    queryFn: () => getTerritories(),
    staleTime: 5 * 60 * 1000,
  });

  const companyOptions = (companiesData?.items ?? []).map((c) => ({
    value: String(c.id),
    label: `${c.shortName} (${c.legalName})`,
  }));

  const territoryOptions = (territoriesData?.items ?? []).map((t) => ({
    value: String(t.id),
    label: `${t.territoryCode} — ${t.territoryName ?? t.territoryCode}`,
  }));

  const form = useForm<Record<string, unknown>>({
    initialValues: {
      senderCompanyId: rights?.senderCompanyId ? String(rights.senderCompanyId) : null,
      receiverCompanyId: rights?.receiverCompanyId ? String(rights.receiverCompanyId) : null,
      territoryId: rights?.territoryId ? String(rights.territoryId) : null,
      docNumber: rights?.docNumber ?? '',
      docStart: rights?.docStart ?? '',
      docEnd: rights?.docEnd ?? '',
      share: rights?.share ?? '',
    },
    validate: {
      senderCompanyId: (v) => (!v ? 'Выберите компанию-отправителя' : null),
      receiverCompanyId: (v) => (!v ? 'Выберите компанию-получателя' : null),
      territoryId: (v) => (!v ? 'Выберите территорию' : null),
      docNumber: (v) => optMaxLen(DbMax.groupMetaRights.docNumber, 'Номер договора')(String(v ?? '')),
      docStart: (v) => combine(required('Укажите дату начала'), dateIsoOptional('Дата начала'))(String(v ?? '')),
      docEnd: (v, values) => {
        const base = combine(required('Укажите дату окончания'), dateIsoOptional('Дата окончания'))(String(v ?? ''));
        if (base) return base;
        const start = String(values.docStart ?? '');
        if (start && String(v) <= start) return 'Дата окончания должна быть позже даты начала';
        return null;
      },
      share: (v) => {
        if (v === '' || v === null || v === undefined) return 'Укажите долю (%)';
        const n = typeof v === 'number' ? v : Number(v);
        const base = sharePercent('Доля')(n);
        if (base) return base;
        if (n < 1) return 'Доля должна быть от 1 до 100%';
        return null;
      },
    },
  });

  const handleSave = async (values: Record<string, unknown>) => {
    setSaving(true);
    const dto: UpdateGroupMetaRightsDto = {
      senderCompanyId: values.senderCompanyId ? Number(values.senderCompanyId) : null,
      receiverCompanyId: values.receiverCompanyId ? Number(values.receiverCompanyId) : null,
      territoryId: values.territoryId ? Number(values.territoryId) : null,
      docNumber: (values.docNumber as string) || null,
      docStart: (values.docStart as string) || null,
      docEnd: (values.docEnd as string) || null,
      share: values.share !== '' ? Number(values.share) : null,
    };
    try {
      await boUpdateMetaRights(taskId, groupId, dto);
      onSaved();
      qc.invalidateQueries({ queryKey: ['group-suspenses', groupId] });
      notifications.show({ title: 'Сохранено', message: 'Метаправа обновлены', color: 'green' });
    } catch (e) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setSaving(false);
    }
  };

  return (
    <Paper withBorder radius="md" p="md">
      <form onSubmit={form.onSubmit(handleSave)}>
        <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
          <Select label="Компания-отправитель *" searchable clearable data={companyOptions}
            value={form.values.senderCompanyId as string | null}
            onChange={(v) => form.setFieldValue('senderCompanyId', v)}
            error={form.errors.senderCompanyId} />
          <Select label="Компания-получатель *" searchable clearable data={companyOptions}
            value={form.values.receiverCompanyId as string | null}
            onChange={(v) => form.setFieldValue('receiverCompanyId', v)}
            error={form.errors.receiverCompanyId} />
          <Select label="Территория *" searchable clearable data={territoryOptions}
            value={form.values.territoryId as string | null}
            onChange={(v) => form.setFieldValue('territoryId', v)}
            error={form.errors.territoryId} />
          <TextInput label="Номер договора" {...form.getInputProps('docNumber')} />
          <TextInput label="Дата начала" type="date" {...form.getInputProps('docStart')} />
          <TextInput label="Дата окончания" type="date" {...form.getInputProps('docEnd')} />
          <NumberInput label="Доля (%) *" min={1} max={100} step={0.01}
            value={form.values.share as number}
            onChange={(v) => form.setFieldValue('share', v)}
            error={form.errors.share} />
        </SimpleGrid>
        <Group justify="flex-end" mt="md">
          <Button type="submit" loading={saving} size="sm">Сохранить</Button>
        </Group>
      </form>
    </Paper>
  );
}

// ── Action buttons ────────────────────────────────────────────────────────────

function PossibleProductsButton({ taskId, groupId, onLinked }: {
  taskId: number; groupId: number; onLinked: () => void;
}) {
  const [opened, { open, close }] = useDisclosure(false);
  const [page, setPage] = useState(1);
  const [linking, setLinking] = useState<number | null>(null);
  const [detailProductId, setDetailProductId] = useState<number | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['bo-possible-products', taskId, page],
    queryFn: () => boGetPossibleProducts(taskId, { pageNumber: page, pageSize: 10 }),
    enabled: opened,
  });

  const qc = useQueryClient();

  const handleLink = async (product: CatalogProduct) => {
    setLinking(product.id);
    try {
      await boLinkProduct(taskId, product.id);
      qc.invalidateQueries({ queryKey: ['bo-tasks'] });
      onLinked();
      close();
      notifications.show({ title: 'Продукт привязан', message: `${product.productName ?? '#' + product.id}`, color: 'green' });
    } catch (e) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setLinking(null);
    }
  };

  const modalTitle = (
    <Group gap="xs">
      <Text fw={600} size="md">Возможные продукты</Text>
      <Tooltip
        label="Система автоматически ищет в каталоге продукты, похожие на суспенсы группы — по исполнителю, названию, ISRC и баркоду. Если нужный продукт уже есть в каталоге, его можно привязать к группе вместо создания нового."
        multiline
        w={320}
        position="bottom"
      >
        <IconInfoCircle size={16} style={{ cursor: 'help', color: 'var(--mantine-color-dimmed)', marginTop: 2 }} />
      </Tooltip>
    </Group>
  );

  return (
    <>
      <Button leftSection={<IconSearch size={14} />} variant="light" onClick={open} size="sm">
        Найти продукт
      </Button>
      <Modal opened={opened} onClose={close} title={modalTitle} size="xl" centered>
        <Stack gap="md">
          {isLoading ? <Center py="xl"><Loader size="sm" /></Center> :
           !data?.items.length ? <Text c="dimmed" ta="center">Продукты не найдены</Text> : (
            <>
              <ScrollArea>
                <Table striped highlightOnHover style={{ minWidth: 600 }}>
                  <Table.Thead>
                    <Table.Tr>
                      <ResizableTh>ID</ResizableTh><ResizableTh>Название</ResizableTh>
                      <ResizableTh>Артист</ResizableTh><ResizableTh>ISRC</ResizableTh>
                      <ResizableTh>Баркод</ResizableTh><ResizableTh />
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {data.items.map((p) => (
                      <Table.Tr key={p.id}>
                        <Table.Td>{p.id}</Table.Td>
                        <Table.Td>{p.productName ?? '—'}</Table.Td>
                        <Table.Td>{p.artist ?? '—'}</Table.Td>
                        <Table.Td>{p.isrc ?? '—'}</Table.Td>
                        <Table.Td>{p.barcode ?? '—'}</Table.Td>
                        <Table.Td>
                          <Group gap={6}>
                            <Tooltip label="Подробнее о продукте">
                              <Button
                                size="xs"
                                variant="subtle"
                                color="gray"
                                px={6}
                                onClick={() => setDetailProductId(p.id)}
                              >
                                <IconFileInfo size={14} />
                              </Button>
                            </Tooltip>
                            <Button size="xs" loading={linking === p.id}
                              leftSection={<IconLink size={12} />}
                              onClick={() => handleLink(p)}>
                              Привязать
                            </Button>
                          </Group>
                        </Table.Td>
                      </Table.Tr>
                    ))}
                  </Table.Tbody>
                </Table>
              </ScrollArea>
              <Pagination value={page} onChange={setPage} total={Math.max(1, data.totalPages)} size="sm" />
            </>
          )}
        </Stack>
      </Modal>
      <ProductDetailModal productId={detailProductId} onClose={() => setDetailProductId(null)} />
    </>
  );
}

function CompleteTaskButton({ taskId, group, onDone }: { taskId: number; group: any; onDone: () => void }) {
  const [opened, { open, close }] = useDisclosure(false);
  const [loading, setLoading] = useState(false);
  const qc = useQueryClient();
  const r = group?.groupMetaRights;

  const handle = async () => {
    setLoading(true);
    try {
      await boCompleteTask(taskId);
      qc.invalidateQueries({ queryKey: ['bo-tasks'] });
      notifications.show({ title: 'Задание завершено', message: 'Права созданы, группа переведена в статус 88', color: 'green', icon: <IconCheck size={14} /> });
      close();
      onDone();
    } catch (e) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setLoading(false);
    }
  };

  const rows: { label: string; value: string | number | null | undefined }[] = [
    { label: 'Компания-отправитель', value: r?.senderCompany?.shortName ?? r?.senderCompanyId },
    { label: 'Компания-получатель', value: r?.receiverCompany?.shortName ?? r?.receiverCompanyId },
    { label: 'Территория', value: r?.territoryCode },
    { label: 'Номер договора', value: r?.docNumber },
    { label: 'Период', value: r?.docStart && r?.docEnd ? `${r.docStart} — ${r.docEnd}` : r?.docStart ?? r?.docEnd },
    { label: 'Доля', value: r?.share != null ? `${r.share}%` : null },
  ];

  return (
    <>
      <Button leftSection={<IconCheck size={14} />} color="green" onClick={open} size="sm">
        Завершить задание
      </Button>
      <Modal opened={opened} onClose={close} title="Подтверждение завершения задания" centered radius="md" size="md">
        <Stack gap="md">
          <Text size="sm" c="dimmed">
            В каталоге будет создана запись прав для продукта{' '}
            <strong>{group?.catalogProduct?.productName ?? `#${group?.catalogProductId}`}</strong>.
            Задание закроется, группа перейдёт в статус 88.
          </Text>
          <Paper withBorder radius="md" p="sm">
            <Stack gap={6}>
              {rows.map(({ label, value }) => (
                <Group key={label} gap="xs" align="flex-start">
                  <Text size="xs" c="dimmed" w={160} style={{ flexShrink: 0 }}>{label}</Text>
                  <Text size="sm" fw={value ? 500 : 400} c={value ? undefined : 'dimmed'}>
                    {value ?? '—'}
                  </Text>
                </Group>
              ))}
            </Stack>
          </Paper>
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={close}>Отмена</Button>
            <Button color="green" loading={loading} onClick={handle}>Завершить</Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}

function ReturnButton({ taskId, onDone }: { taskId: number; onDone: () => void }) {
  const [opened, { open, close }] = useDisclosure(false);
  const [loading, setLoading] = useState(false);
  const qc = useQueryClient();

  const handle = async () => {
    setLoading(true);
    try {
      await boReturnGroup(taskId);
      qc.invalidateQueries({ queryKey: ['bo-tasks'] });
      notifications.show({ title: 'Возвращено', message: 'Группа возвращена оператору', color: 'blue' });
      close();
      onDone();
    } catch (e) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <Button leftSection={<IconArrowBack size={14} />} variant="light" color="orange" onClick={open} size="sm">
        Вернуть оператору
      </Button>
      <Modal opened={opened} onClose={close} title="Вернуть группу оператору" centered>
        <Stack gap="md">
          <Text size="sm">Группа вернётся в обычную обработку (120→15 или 320→16). Задание закроется.</Text>
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={close}>Отмена</Button>
            <Button color="orange" loading={loading} onClick={handle}>Вернуть</Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}

function DeleteButton({ taskId, onDone }: { taskId: number; onDone: () => void }) {
  const [opened, { open, close }] = useDisclosure(false);
  const [loading, setLoading] = useState(false);
  const qc = useQueryClient();

  const handle = async () => {
    setLoading(true);
    try {
      await boDeleteGroup(taskId);
      qc.invalidateQueries({ queryKey: ['bo-tasks'] });
      notifications.show({ title: 'Удалено', message: 'Группа и суспенсы архивированы', color: 'gray' });
      close();
      onDone();
    } catch (e) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <Button leftSection={<IconTrash size={14} />} variant="light" color="red" onClick={open} size="sm">
        Удалить группу
      </Button>
      <Modal opened={opened} onClose={close} title="Удалить группу" centered>
        <Stack gap="md">
          <Alert color="red" radius="md">
            Группа и все суспенсы будут архивированы без возможности восстановления. Используйте только если стримы точно не принадлежат компании.
          </Alert>
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={close}>Отмена</Button>
            <Button color="red" loading={loading} onClick={handle}>Удалить</Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}
