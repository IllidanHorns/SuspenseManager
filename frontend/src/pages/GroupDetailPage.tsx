import { useState, useEffect } from 'react';
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
  Loader,
  Center,
  Alert,
  Box,
  Divider,
  Modal,
  TextInput,
  Textarea,
  NumberInput,
  Select,
  Tooltip,
  Anchor,
  Pagination,
  SimpleGrid,
  Badge,
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
  IconShieldCheck,
  IconChevronUp,
  IconChevronDown,
  IconInfoCircle,
  IconFileInfo,
} from '@tabler/icons-react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { isApiRequestError } from '../api/apiError';
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
  createGroupRights,
  searchCatalogRights,
  copyRightsToProduct,
} from '../api/processing';
import { getCompanies } from '../api/companies';
import { getTerritories } from '../api/territories';
import { fmtDate, fmtDateTime, downloadBlob } from '../utils/format';
import {
  getMissingCatalogProductIdentityFields,
  getMissingProductIdentityForQuickCatalog,
} from '../utils/requiredProductIdentity';
import {
  DbMax,
  combine,
  dateIsoOptional,
  optMaxLen,
  required,
  sharePercent,
} from '../utils/fieldValidation';
import { StatusBadge } from '../components/common/StatusBadge';
import { PageSizeSelect } from '../components/common/PageSizeSelect';
import { ResizableTh } from '../components/common/ResizableTh';
import { CollapsibleFilters } from '../components/common/CollapsibleFilters';
import { SearchRightsCatalogModal } from '../components/groups/SearchRightsCatalogModal';
import { ProductDetailModal } from '../components/groups/ProductDetailModal';
import { useDefaultPageSize } from '../hooks/useDefaultPageSize';
import { usePermissions } from '../hooks/usePermissions';
import { PermissionCodes } from '../utils/permissions';
import type {
  GroupMetadata,
  GroupMetaRights,
  CatalogProduct,
  SuspenseLine,
  Territory,
} from '../types';

/** Выручка по строке: кол-во × цена за стрим × курс (как на бэкенде в агрегатах группы). */
function lineRevenueRub(s: SuspenseLine): number {
  const rate = typeof s.exchangeRate === 'number' ? s.exchangeRate : Number(s.exchangeRate);
  return s.qty * (s.ppd ?? 0) * (Number.isFinite(rate) ? rate : 0);
}

function fmtRub(n: number): string {
  return n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function cell(v: string | number | null | undefined): string {
  if (v === null || v === undefined) return '—';
  const t = typeof v === 'number' ? String(v) : String(v).trim();
  return t === '' ? '—' : t;
}

type GroupSuspenseSortField = 'Qty' | 'Ppd' | 'Revenue';

function SortableResizableTh({
  label,
  field,
  sortBy,
  sortDirection,
  onSort,
}: {
  label: string;
  field: GroupSuspenseSortField;
  sortBy: GroupSuspenseSortField | null;
  sortDirection: 'asc' | 'desc';
  onSort: (f: GroupSuspenseSortField) => void;
}) {
  const active = sortBy === field;
  return (
    <ResizableTh>
      <Group
        gap={4}
        wrap="nowrap"
        align="center"
        justify="space-between"
        pr={4}
        onClick={() => onSort(field)}
        style={{ cursor: 'pointer' }}
      >
        <Text size="xs" fw={600}>{label}</Text>
        {active ? (
          sortDirection === 'asc' ? <IconChevronUp size={14} /> : <IconChevronDown size={14} />
        ) : (
          <Box w={14} />
        )}
      </Group>
    </ResizableTh>
  );
}

interface GroupSuspenseFilterForm {
  isrc: string;
  artist: string;
  operator: string;
  barcode: string;
  catalogNumber: string;
  trackTitle: string;
  genre: string;
  senderCompany: string;
  recipientCompany: string;
  territoryCode: string;
  agreementType: string;
  agreementNumber: string;
  qtyMin: string;
  qtyMax: string;
  ppdMin: string;
  ppdMax: string;
  revenueMin: string;
  revenueMax: string;
}

const EMPTY_GROUP_SUSPENSE_FILTERS: GroupSuspenseFilterForm = {
  isrc: '',
  artist: '',
  operator: '',
  barcode: '',
  catalogNumber: '',
  trackTitle: '',
  genre: '',
  senderCompany: '',
  recipientCompany: '',
  territoryCode: '',
  agreementType: '',
  agreementNumber: '',
  qtyMin: '',
  qtyMax: '',
  ppdMin: '',
  ppdMax: '',
  revenueMin: '',
  revenueMax: '',
};

function buildGroupSuspenseApplied(
  p: GroupSuspenseFilterForm,
  compactNoProduct: boolean,
): Record<string, string> {
  const f: Record<string, string> = {};
  const t = (s: string) => s.trim();
  if (t(p.isrc)) f.Isrc_contains = t(p.isrc);
  if (t(p.artist)) f.Artist_contains = t(p.artist);
  if (t(p.operator)) f.Operator_contains = t(p.operator);
  if (t(p.barcode)) f.Barcode_contains = t(p.barcode);
  if (t(p.catalogNumber)) f.CatalogNumber_contains = t(p.catalogNumber);
  if (t(p.trackTitle)) f.TrackTitle_contains = t(p.trackTitle);
  if (t(p.genre)) f.Genre_contains = t(p.genre);
  if (t(p.senderCompany)) f.SenderCompany_contains = t(p.senderCompany);
  if (t(p.recipientCompany)) f.RecipientCompany_contains = t(p.recipientCompany);
  if (t(p.territoryCode)) f.TerritoryCode_contains = t(p.territoryCode);
  if (!compactNoProduct) {
    if (t(p.agreementType)) f.AgreementType_contains = t(p.agreementType);
    if (t(p.agreementNumber)) f.AgreementNumber_contains = t(p.agreementNumber);
  }
  if (t(p.qtyMin)) f.Qty_gte = t(p.qtyMin);
  if (t(p.qtyMax)) f.Qty_lte = t(p.qtyMax);
  if (t(p.ppdMin)) f.Ppd_gte = t(p.ppdMin);
  if (t(p.ppdMax)) f.Ppd_lte = t(p.ppdMax);
  if (t(p.revenueMin)) f.Revenue_gte = t(p.revenueMin);
  if (t(p.revenueMax)) f.Revenue_lte = t(p.revenueMax);
  return f;
}

// Metadata Form

function MetadataTab({
  groupId,
  metadata,
  readOnly,
  linkedToProduct,
}: {
  groupId: number;
  metadata: GroupMetadata | null;
  readOnly: boolean;
  linkedToProduct?: boolean;
}) {
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

  // initialValues срабатывают только при монтировании; метаданные приходят позже из useQuery
  useEffect(() => {
    if (metadata === undefined) return;
    form.setValues({
      title: metadata?.title ?? '',
      artist: metadata?.artist ?? '',
      isrc: metadata?.isrc ?? '',
      barcode: metadata?.barcode ?? '',
      catalogNumber: metadata?.catalogNumber ?? '',
      genre: metadata?.genre ?? '',
      description: metadata?.description ?? '',
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps -- синхронизация с ответом сервера
  }, [metadata]);

  const handleSave = async (values: typeof form.values) => {
    if (readOnly) return;
    setSaving(true);
    try {
      await updateMetadata(groupId, values);
      await qc.invalidateQueries({ queryKey: ['metadata', groupId] });
      await qc.invalidateQueries({ queryKey: ['group-suspenses', groupId] });
      await qc.invalidateQueries({ queryKey: ['group', groupId] });
      await qc.invalidateQueries({ queryKey: ['groups'] });
      notifications.show({ title: 'Сохранено', message: 'Метаданные обновлены', color: 'green', icon: <IconCircleCheck size={16} /> });
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка сохранения', color: 'red' });
    } finally {
      setSaving(false);
    }
  };

  if (readOnly) {
    return (
      <Stack gap="md">
        <Alert color="blue" variant="light" icon={<IconCircleCheck size={18} />}>
          {linkedToProduct
            ? 'Группа привязана к продукту каталога. Поля продукта показываются из метаданных (синхронизированы с каталогом) и не редактируются отдельно.'
            : 'Просмотр метаданных. Для редактирования необходимо право быстрой каталогизации.'}
        </Alert>
        <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
          <Box>
            <Text size="xs" c="dimmed">
              Название
            </Text>
            <Text size="sm">{cell(metadata?.title)}</Text>
          </Box>
          <Box>
            <Text size="xs" c="dimmed">
              Исполнитель
            </Text>
            <Text size="sm">{cell(metadata?.artist)}</Text>
          </Box>
          <Box>
            <Text size="xs" c="dimmed">
              ISRC
            </Text>
            <Text size="sm">{cell(metadata?.isrc)}</Text>
          </Box>
          <Box>
            <Text size="xs" c="dimmed">
              Баркод
            </Text>
            <Text size="sm">{cell(metadata?.barcode)}</Text>
          </Box>
          <Box>
            <Text size="xs" c="dimmed">
              Кат. номер
            </Text>
            <Text size="sm">{cell(metadata?.catalogNumber)}</Text>
          </Box>
          <Box>
            <Text size="xs" c="dimmed">
              Жанр
            </Text>
            <Text size="sm">{cell(metadata?.genre)}</Text>
          </Box>
          <Box>
            <Text size="xs" c="dimmed">
              Тип продукта
            </Text>
            <Text size="sm">{cell(metadata?.productTypeCode)}</Text>
          </Box>
          <Box>
            <Text size="xs" c="dimmed">
              Описание типа
            </Text>
            <Text size="sm">{cell(metadata?.productTypeDesc)}</Text>
          </Box>
          <Box>
            <Text size="xs" c="dimmed">
              Дата релиза
            </Text>
            <Text size="sm">{metadata?.releaseDate ? fmtDate(metadata.releaseDate) : '—'}</Text>
          </Box>
        </SimpleGrid>
        <Box>
          <Text size="xs" c="dimmed">
            Описание
          </Text>
          <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
            {cell(metadata?.description)}
          </Text>
        </Box>
      </Stack>
    );
  }

  return (
    <form onSubmit={form.onSubmit(handleSave)}>
      <Stack gap="md">
        <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
          <TextInput label="Название" {...form.getInputProps('title')} />
          <TextInput label="Исполнитель" {...form.getInputProps('artist')} />
          <TextInput label="ISRC" {...form.getInputProps('isrc')} />
          <TextInput label="Баркод" {...form.getInputProps('barcode')} />
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

// Meta-Rights Form

function MetaRightsTab({ groupId, metaRights, readOnly = false }: { groupId: number; metaRights: GroupMetaRights | null; readOnly?: boolean }) {
  const qc = useQueryClient();
  const [saving, setSaving] = useState(false);

  const { data: companiesData } = useQuery({
    queryKey: ['companies'],
    queryFn: () => getCompanies({ pageSize: 500 }),
    staleTime: 5 * 60 * 1000,
  });

  const { data: territoriesData } = useQuery({
    queryKey: ['territories'],
    queryFn: getTerritories,
    staleTime: 5 * 60 * 1000,
  });

  const companies = companiesData?.items ?? [];
  const territories = territoriesData?.items ?? [];

  const companyOptions = companies.map((c) => ({
    value: String(c.id),
    label: c.shortName || c.legalName,
  }));

  const territoryOptions = territories.map((t) => ({
    value: String(t.id),
    label: `${t.territoryCode}${t.territoryName ? ` — ${t.territoryName}` : ''}`,
  }));

  const form = useForm({
    initialValues: {
      senderCompanyId:   metaRights?.senderCompanyId   ? String(metaRights.senderCompanyId)   : (null as string | null),
      receiverCompanyId: metaRights?.receiverCompanyId ? String(metaRights.receiverCompanyId) : (null as string | null),
      territoryId:       metaRights?.territoryId       ? String(metaRights.territoryId)       : (null as string | null),
      territoryCode:     metaRights?.territoryCode ?? '',
      territoryDesc:     metaRights?.territoryDesc ?? '',
      docNumber:         metaRights?.docNumber ?? '',
      docType:           metaRights?.docType ?? '',
      docDate:           metaRights?.docDate ?? '',
      docStart:          metaRights?.docStart ?? '',
      docEnd:            metaRights?.docEnd ?? '',
      share:             metaRights?.share ?? (null as number | null),
    },
    validate: {
      senderCompanyId: (v) => (!v ? 'Выберите компанию-отправителя' : null),
      receiverCompanyId: (v) => (!v ? 'Выберите компанию-получателя' : null),
      territoryId: (v) => (!v ? 'Выберите территорию' : null),
      territoryCode: optMaxLen(DbMax.groupMetaRights.territoryCode, 'Код территории'),
      territoryDesc: optMaxLen(DbMax.groupMetaRights.territoryDesc, 'Описание территории'),
      docNumber: optMaxLen(DbMax.groupMetaRights.docNumber, 'Номер договора'),
      docType: optMaxLen(DbMax.groupMetaRights.docType, 'Тип договора'),
      docDate: (v, values) => {
        const base = dateIsoOptional('Дата договора')(v);
        if (base) return base;
        if (v && values.docStart && v > values.docStart) return 'Дата договора должна быть не позже даты начала действия';
        return null;
      },
      docStart: combine(required('Укажите дату начала действия'), dateIsoOptional('Дата начала')),
      docEnd: (v, values) => {
        const base = combine(required('Укажите дату окончания действия'), dateIsoOptional('Дата окончания'))(v);
        if (base) return base;
        if (values.docStart && v && v <= values.docStart) return 'Дата окончания должна быть позже даты начала';
        return null;
      },
      share: (v) => {
        const base = sharePercent('Доля')(v);
        if (base) return base;
        if (v !== null && v !== undefined && Number(v) < 1) return 'Доля должна быть от 1 до 100%';
        return null;
      },
    },
  });

  useEffect(() => {
    if (metaRights === undefined) return;
    form.setValues({
      senderCompanyId:   metaRights?.senderCompanyId   ? String(metaRights.senderCompanyId)   : null,
      receiverCompanyId: metaRights?.receiverCompanyId ? String(metaRights.receiverCompanyId) : null,
      territoryId:       metaRights?.territoryId       ? String(metaRights.territoryId)       : null,
      territoryCode:     metaRights?.territoryCode ?? '',
      territoryDesc:     metaRights?.territoryDesc ?? '',
      docNumber:         metaRights?.docNumber ?? '',
      docType:           metaRights?.docType ?? '',
      docDate:           metaRights?.docDate ?? '',
      docStart:          metaRights?.docStart ?? '',
      docEnd:            metaRights?.docEnd ?? '',
      share:             metaRights?.share ?? null,
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [metaRights]);

  // При выборе территории — автозаполняем код и описание
  const handleTerritoryChange = (val: string | null) => {
    form.setFieldValue('territoryId', val);
    if (val) {
      const t: Territory | undefined = territories.find((x) => String(x.id) === val);
      if (t) {
        form.setFieldValue('territoryCode', t.territoryCode);
        form.setFieldValue('territoryDesc', t.territoryName ?? '');
      }
    }
  };

  const handleSave = async (values: typeof form.values) => {
    setSaving(true);
    try {
      // Преобразуем строковые ID в числа и пустые даты в null
      const payload = {
        senderCompanyId:   values.senderCompanyId   ? Number(values.senderCompanyId)   : null,
        receiverCompanyId: values.receiverCompanyId ? Number(values.receiverCompanyId) : null,
        territoryId:       values.territoryId       ? Number(values.territoryId)       : null,
        territoryCode:     values.territoryCode || null,
        territoryDesc:     values.territoryDesc || null,
        docNumber:         values.docNumber || null,
        docType:           values.docType || null,
        docDate:           values.docDate || null,
        docStart:          values.docStart || null,
        docEnd:            values.docEnd || null,
        share:             values.share ?? null,
      };
      await updateMetaRights(groupId, payload);
      await qc.invalidateQueries({ queryKey: ['meta-rights', groupId] });
      await qc.invalidateQueries({ queryKey: ['group', groupId] });
      await qc.invalidateQueries({ queryKey: ['groups'] });
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
        <Alert icon={<IconAlertCircle size={14} />} color="blue" variant="light" radius="md">
          Заполните обязательные поля (*) и нажмите «Создать права» — система добавит права в каталог
          и переведёт группу в статус 88. Или используйте «Найти права», чтобы скопировать права
          с похожего продукта.
        </Alert>

        <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
          <Select
            label="Компания-отправитель *"
            placeholder="Выберите компанию"
            data={companyOptions}
            searchable
            clearable
            value={form.values.senderCompanyId}
            onChange={(v) => form.setFieldValue('senderCompanyId', v)}
            error={form.errors.senderCompanyId}
          />
          <Select
            label="Компания-получатель *"
            placeholder="Выберите компанию"
            data={companyOptions}
            searchable
            clearable
            value={form.values.receiverCompanyId}
            onChange={(v) => form.setFieldValue('receiverCompanyId', v)}
            error={form.errors.receiverCompanyId}
          />
          <Select
            label="Территория *"
            placeholder="Выберите территорию"
            data={territoryOptions}
            searchable
            clearable
            value={form.values.territoryId}
            onChange={handleTerritoryChange}
            error={form.errors.territoryId}
          />
          <TextInput
            label="Код территории"
            description="Заполняется автоматически при выборе территории"
            {...form.getInputProps('territoryCode')}
          />
          <TextInput label="Номер договора" {...form.getInputProps('docNumber')} />
          <TextInput label="Тип договора" {...form.getInputProps('docType')} />
          <TextInput label="Дата договора" type="date" {...form.getInputProps('docDate')} />
          <TextInput label="Действует с *" type="date" {...form.getInputProps('docStart')} />
          <TextInput label="Действует по *" type="date" {...form.getInputProps('docEnd')} />
          <NumberInput
            label="Доля (%) *"
            min={0}
            max={100}
            decimalScale={2}
            {...form.getInputProps('share')}
          />
        </SimpleGrid>

        <Text size="xs" c="dimmed">* — обязательные поля для валидации</Text>

        {!readOnly && (
          <Group justify="flex-end">
            <Button type="submit" loading={saving} color="violet" leftSection={<IconEdit size={14} />}>
              Сохранить права
            </Button>
          </Group>
        )}
      </Stack>
    </form>
  );
}

// Possible Products Modal

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
  const [pageSize, setPageSize] = useDefaultPageSize();
  const [linking, setLinking] = useState<number | null>(null);
  const [detailProductId, setDetailProductId] = useState<number | null>(null);
  const handlePageSizeChange = (v: number) => { setPageSize(v); setPage(1); };
  const { data, isLoading } = useQuery({
    queryKey: ['possible-products', groupId, page, pageSize],
    queryFn: () => getPossibleProducts(groupId, { pageNumber: page, pageSize }),
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
      const incomplete = isApiRequestError(e) && e.businessCode === 'CATALOG_PRODUCT_INCOMPLETE';
      notifications.show({
        title: incomplete ? 'Нельзя привязать этот продукт' : 'Ошибка',
        message: e instanceof Error ? e.message : 'Ошибка привязки',
        color: incomplete ? 'orange' : 'red',
      });
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
      <Modal opened={opened} onClose={onClose} title={modalTitle} size="xl" radius="md">
        {isLoading ? <Center py="xl"><Loader color="indigo" /></Center> : (
          <Stack gap="md">
            <Table striped highlightOnHover layout="fixed" style={{ minWidth: 640 }}>
              <Table.Thead>
                <Table.Tr>
                  <ResizableTh>ID</ResizableTh>
                  <ResizableTh>Название</ResizableTh>
                  <ResizableTh>Исполнитель</ResizableTh>
                  <ResizableTh>ISRC</ResizableTh>
                  <ResizableTh minWidth={140} title="Пустые обязательные поля в каталоге">
                    Не хватает
                  </ResizableTh>
                  <ResizableTh style={{ width: 150 }} />
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {(data?.items ?? []).map((p) => {
                  const missing = getMissingCatalogProductIdentityFields(p);
                  const canLink = missing.length === 0;
                  return (
                  <Table.Tr key={p.id}>
                    <Table.Td>{p.id}</Table.Td>
                    <Table.Td><Text size="sm" lineClamp={2}>{p.productName ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm" lineClamp={2}>{p.artist ?? '—'}</Text></Table.Td>
                    <Table.Td><Text size="sm" ff="monospace">{p.isrc ?? '—'}</Text></Table.Td>
                    <Table.Td style={{ maxWidth: 200 }}>
                      {canLink ? (
                        <Text size="xs" c="teal">все поля</Text>
                      ) : (
                        <Group gap={4} wrap="wrap">
                          {missing.map((m) => (
                            <Badge key={m} size="xs" variant="light" color="orange">{m}</Badge>
                          ))}
                        </Group>
                      )}
                    </Table.Td>
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
                        <Tooltip
                          label="Доработайте карточку в каталоге или отправьте группу в бэк-офис"
                          disabled={canLink}
                        >
                          <Button
                            size="xs"
                            color="indigo"
                            variant="light"
                            loading={linking === p.id}
                            disabled={!canLink}
                            onClick={() => handleLink(p)}
                          >
                            Привязать
                          </Button>
                        </Tooltip>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                  );
                })}
              </Table.Tbody>
            </Table>
            <Group justify="space-between" pt="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
              <Text size="sm" c="dimmed">Всего: {data?.totalCount ?? 0}</Text>
              <Group gap="sm">
                <PageSizeSelect value={pageSize} onChange={handlePageSizeChange} />
                <Pagination value={page} onChange={setPage} total={Math.max(1, data?.totalPages ?? 1)} size="sm" />
              </Group>
            </Group>
          </Stack>
        )}
      </Modal>
      <ProductDetailModal productId={detailProductId} onClose={() => setDetailProductId(null)} />
    </>
  );
}

// Suspenses Tab

function SuspensesTab({ groupId, businessStatus }: { groupId: number; businessStatus: number }) {
  /** Сохранённые группы «нет продукта» (15) — договор/валюта/курс к правам не относятся, не показываем. */
  const compactNoProduct = businessStatus === 15;
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useDefaultPageSize();
  const handlePageSizeChange = (v: number) => { setPageSize(v); setPage(1); };
  const [pending, setPending] = useState<GroupSuspenseFilterForm>(EMPTY_GROUP_SUSPENSE_FILTERS);
  const [applied, setApplied] = useState<Record<string, string>>({});
  const [sortBy, setSortBy] = useState<GroupSuspenseSortField | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const hasActive = Object.keys(applied).length > 0;

  const setField = (key: keyof GroupSuspenseFilterForm) => (e: React.ChangeEvent<HTMLInputElement>) => {
    setPending((prev) => ({ ...prev, [key]: e.target.value }));
  };

  const applyFilters = () => {
    setApplied(buildGroupSuspenseApplied(pending, compactNoProduct));
    setPage(1);
  };

  const resetFilters = () => {
    setPending(EMPTY_GROUP_SUSPENSE_FILTERS);
    setApplied({});
    setPage(1);
  };

  const onKey = (e: React.KeyboardEvent) => { if (e.key === 'Enter') applyFilters(); };

  const toggleSort = (field: GroupSuspenseSortField) => {
    if (sortBy !== field) {
      setSortBy(field);
      setSortDirection('asc');
    } else if (sortDirection === 'asc') {
      setSortDirection('desc');
    } else {
      setSortBy(null);
      setSortDirection('asc');
    }
    setPage(1);
  };

  const { data, isLoading } = useQuery({
    queryKey: ['group-suspenses', groupId, page, pageSize, applied, sortBy, sortDirection],
    queryFn: () => getGroupSuspenses(groupId, {
      pageNumber: page,
      pageSize,
      sortBy: sortBy ?? undefined,
      sortDirection: sortBy ? sortDirection : undefined,
      Filters: applied,
    }),
  });

  const rows = data?.items ?? [];

  return (
    <Stack gap="md">
      <CollapsibleFilters activeCount={Object.keys(applied).length}>
        <Paper withBorder radius="md" p="sm">
          <Stack gap="sm">
            <SimpleGrid cols={{ base: 1, sm: 2, md: 4 }} spacing="xs">
              <TextInput size="xs" label="ISRC" placeholder="подстрока" value={pending.isrc} onChange={setField('isrc')} onKeyDown={onKey} />
              <TextInput size="xs" label="Баркод" placeholder="подстрока" value={pending.barcode} onChange={setField('barcode')} onKeyDown={onKey} />
              <TextInput size="xs" label="Кат. номер" placeholder="подстрока" value={pending.catalogNumber} onChange={setField('catalogNumber')} onKeyDown={onKey} />
              <TextInput size="xs" label="Исполнитель" placeholder="подстрока" value={pending.artist} onChange={setField('artist')} onKeyDown={onKey} />
              <TextInput size="xs" label="Название" placeholder="подстрока" value={pending.trackTitle} onChange={setField('trackTitle')} onKeyDown={onKey} />
              <TextInput size="xs" label="Жанр" placeholder="подстрока" value={pending.genre} onChange={setField('genre')} onKeyDown={onKey} />
              <TextInput size="xs" label="Оператор" placeholder="подстрока" value={pending.operator} onChange={setField('operator')} onKeyDown={onKey} />
              <TextInput size="xs" label="Отправитель" placeholder="подстрока" value={pending.senderCompany} onChange={setField('senderCompany')} onKeyDown={onKey} />
              <TextInput size="xs" label="Получатель" placeholder="подстрока" value={pending.recipientCompany} onChange={setField('recipientCompany')} onKeyDown={onKey} />
              <TextInput size="xs" label="Территория" placeholder="подстрока" value={pending.territoryCode} onChange={setField('territoryCode')} onKeyDown={onKey} />
              {!compactNoProduct && (
                <>
                  <TextInput size="xs" label="Тип договора" placeholder="подстрока" value={pending.agreementType} onChange={setField('agreementType')} onKeyDown={onKey} />
                  <TextInput size="xs" label="Номер договора" placeholder="подстрока" value={pending.agreementNumber} onChange={setField('agreementNumber')} onKeyDown={onKey} />
                </>
              )}
            </SimpleGrid>
            <SimpleGrid cols={{ base: 1, sm: 2, md: 6 }} spacing="xs">
              <TextInput size="xs" label="Стримы от" placeholder="≥" value={pending.qtyMin} onChange={setField('qtyMin')} onKeyDown={onKey} />
              <TextInput size="xs" label="Стримы до" placeholder="≤" value={pending.qtyMax} onChange={setField('qtyMax')} onKeyDown={onKey} />
              <TextInput size="xs" label="Цена от" placeholder="≥" value={pending.ppdMin} onChange={setField('ppdMin')} onKeyDown={onKey} />
              <TextInput size="xs" label="Цена до" placeholder="≤" value={pending.ppdMax} onChange={setField('ppdMax')} onKeyDown={onKey} />
              <TextInput size="xs" label="Выручка от, ₽" placeholder="≥" value={pending.revenueMin} onChange={setField('revenueMin')} onKeyDown={onKey} />
              <TextInput size="xs" label="Выручка до, ₽" placeholder="≤" value={pending.revenueMax} onChange={setField('revenueMax')} onKeyDown={onKey} />
            </SimpleGrid>
            <Group gap="xs">
              <Button size="xs" leftSection={<IconSearch size={12} />} onClick={applyFilters}>Найти</Button>
              {hasActive && (
                <Button size="xs" variant="subtle" color="gray" leftSection={<IconX size={12} />} onClick={resetFilters}>
                  Сбросить
                </Button>
              )}
            </Group>
          </Stack>
        </Paper>
      </CollapsibleFilters>
      {isLoading ? <Center py="xl"><Loader color="indigo" /></Center> : (
        <>
          <ScrollArea>
            <Table striped highlightOnHover style={{ minWidth: compactNoProduct ? 1280 : 1680 }}>
              <Table.Thead>
                <Table.Tr>
                  <ResizableTh>ID</ResizableTh>
                  <ResizableTh>ISRC</ResizableTh>
                  <ResizableTh>Баркод</ResizableTh>
                  <ResizableTh>Кат. номер</ResizableTh>
                  <ResizableTh>Исполнитель</ResizableTh>
                  <ResizableTh>Название</ResizableTh>
                  <ResizableTh>Жанр</ResizableTh>
                  <ResizableTh>Оператор</ResizableTh>
                  <ResizableTh>Отправитель</ResizableTh>
                  <ResizableTh>Получатель</ResizableTh>
                  <ResizableTh>Территория</ResizableTh>
                  {!compactNoProduct && (
                    <>
                      <ResizableTh>Тип договора</ResizableTh>
                      <ResizableTh>Номер договора</ResizableTh>
                    </>
                  )}
                  <SortableResizableTh
                    label="Кол-во стримов"
                    field="Qty"
                    sortBy={sortBy}
                    sortDirection={sortDirection}
                    onSort={toggleSort}
                  />
                  <SortableResizableTh
                    label="Цена за стрим"
                    field="Ppd"
                    sortBy={sortBy}
                    sortDirection={sortDirection}
                    onSort={toggleSort}
                  />
                  {!compactNoProduct && (
                    <>
                      <ResizableTh>Валюта</ResizableTh>
                      <ResizableTh>Курс</ResizableTh>
                    </>
                  )}
                  <SortableResizableTh
                    label="Выручка, ₽"
                    field="Revenue"
                    sortBy={sortBy}
                    sortDirection={sortDirection}
                    onSort={toggleSort}
                  />
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {rows.map((s: SuspenseLine) => (
                  <Table.Tr key={s.id}>
                    <Table.Td><Text size="sm" fw={600}>{s.id}</Text></Table.Td>
                    <Table.Td><Text size="sm">{cell(s.isrc)}</Text></Table.Td>
                    <Table.Td><Text size="sm">{cell(s.barcode)}</Text></Table.Td>
                    <Table.Td><Text size="sm">{cell(s.catalogNumber)}</Text></Table.Td>
                    <Table.Td><Text size="sm">{cell(s.artist)}</Text></Table.Td>
                    <Table.Td><Text size="sm">{cell(s.trackTitle)}</Text></Table.Td>
                    <Table.Td><Text size="sm">{cell(s.genre)}</Text></Table.Td>
                    <Table.Td><Text size="sm">{cell(s.operator)}</Text></Table.Td>
                    <Table.Td><Text size="sm">{cell(s.senderCompany)}</Text></Table.Td>
                    <Table.Td><Text size="sm">{cell(s.recipientCompany)}</Text></Table.Td>
                    <Table.Td><Text size="sm">{cell(s.territoryCode)}</Text></Table.Td>
                    {!compactNoProduct && (
                      <>
                        <Table.Td><Text size="sm">{cell(s.agreementType)}</Text></Table.Td>
                        <Table.Td><Text size="sm">{cell(s.agreementNumber)}</Text></Table.Td>
                      </>
                    )}
                    <Table.Td><Text size="sm">{s.qty}</Text></Table.Td>
                    <Table.Td><Text size="sm">{s.ppd != null ? String(s.ppd) : '—'}</Text></Table.Td>
                    {!compactNoProduct && (
                      <>
                        <Table.Td><Text size="sm">{cell(s.exchangeCurrency)}</Text></Table.Td>
                        <Table.Td><Text size="sm">{s.exchangeRate}</Text></Table.Td>
                      </>
                    )}
                    <Table.Td><Text size="sm" fw={500}>{fmtRub(lineRevenueRub(s))}</Text></Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
          <Group justify="space-between" px="sm" pt="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {data?.totalCount ?? 0}</Text>
            <Group gap="sm">
              <PageSizeSelect value={pageSize} onChange={handlePageSizeChange} />
              <Pagination value={page} onChange={setPage} total={Math.max(1, data?.totalPages ?? 1)} size="sm" />
            </Group>
          </Group>
        </>
      )}
    </Stack>
  );
}

// Main Page

export function GroupDetailPage() {
  const { id } = useParams<{ id: string }>();
  const groupId = Number(id);
  const navigate = useNavigate();
  const qc = useQueryClient();
  const { hasPermission, hasAnyPermission } = usePermissions();

  const permCatalogFast = hasPermission(PermissionCodes.groupsNoProductCatalogFast);
  const permPossibleProducts = hasPermission(PermissionCodes.groupsNoProductPossibleProducts);
  const permCorrectRights = hasPermission(PermissionCodes.groupsNoRightsCorrectRights);
  const permExport = hasPermission(PermissionCodes.groupsExport);
  const permSendBackoffice = hasAnyPermission([PermissionCodes.groupsNoProductSendBackoffice, PermissionCodes.groupsNoRightsSendBackoffice]);
  const permPostpone = hasAnyPermission([PermissionCodes.groupsNoProductPostpone, PermissionCodes.groupsNoRightsPostpone]);
  const permUngroup = hasAnyPermission([PermissionCodes.groupsNoProductUngroup, PermissionCodes.groupsNoRightsUngroup]);
  const permReturn = hasPermission(PermissionCodes.postponedReturn);

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
  const [searchRightsOpen, { open: openSearchRights, close: closeSearchRights }] = useDisclosure(false);
  const [boOpen, { open: openBo, close: closeBo }] = useDisclosure(false);
  const [postponeOpen, { open: openPostpone, close: closePostpone }] = useDisclosure(false);
  const [ungroupOpen, { open: openUngroup, close: closeUngroup }] = useDisclosure(false);
  const [catalogFastOpen, { open: openCatalogFast, close: closeCatalogFast }] = useDisclosure(false);

  const [boComment, setBoComment] = useState('');
  const [boCommentError, setBoCommentError] = useState<string | null>(null);
  const [postponeReason, setPostponeReason] = useState('');
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  const refreshGroup = async () => {
    await qc.invalidateQueries({ queryKey: ['group', groupId] });
    await qc.invalidateQueries({ queryKey: ['groups'] });
  };

  const runAction = async (key: string, fn: () => Promise<unknown>) => {
    setActionLoading(key);
    try {
      await fn();
      await refreshGroup();
    } catch (e: unknown) {
      notifications.show({ title: 'Ошибка', message: e instanceof Error ? e.message : 'Ошибка', color: 'red' });
    } finally {
      setActionLoading(null);
    }
  };

  const handleCatalogFast = () => {
    const miss = getMissingProductIdentityForQuickCatalog(metadata ?? group?.groupMetaData ?? null);
    if (miss.length) {
      notifications.show({
        title: 'Сначала дозаполните метаданные',
        message: `Для быстрой каталогизации нужны: название, исполнитель, ISRC, баркод, каталожный номер. Не хватает: ${miss.join(', ')}.`,
        color: 'orange',
      });
      return;
    }
    openCatalogFast();
  };

  const handleCatalogFastConfirm = () => {
    closeCatalogFast();
    return runAction('catalog-fast', async () => {
      await catalogFast(groupId);
      notifications.show({ title: 'Продукт создан', message: 'Быстрая каталогизация выполнена', color: 'green' });
    });
  };

  const [createRightsOpen, { open: openCreateRights, close: closeCreateRights }] = useDisclosure(false);

  const handleCreateRightsConfirm = () => {
    closeCreateRights();
    return runAction('create-rights', async () => {
      await createGroupRights(groupId);
      notifications.show({
        title: 'Права созданы',
        message: 'Права добавлены в каталог, статус группы изменён на 88',
        color: 'green',
        icon: <IconCircleCheck size={16} />,
      });
    });
  };

  const handleSendBo = async () => {
    if (!boComment.trim()) {
      setBoCommentError('Описание проблемы обязательно — без него специалист бэк-офиса не сможет понять, что нужно сделать с группой');
      return;
    }
    await runAction('bo', async () => {
      await sendToBackOffice(groupId, boComment);
      notifications.show({ title: 'Отправлено в бэк-офис', message: '', color: 'blue' });
      closeBo();
      setBoComment('');
      setBoCommentError(null);
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
  const statusAllowsUngroup = [15, 16, 30, 32].includes(group.businessStatus);

  const metaForCheck = metadata ?? group.groupMetaData ?? null;
  const missingForQuick = getMissingProductIdentityForQuickCatalog(metaForCheck);

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

          {isPostponed && group.postponeReason && (
            <Paper withBorder radius="md" p="md" w="100%">
              <Text size="xs" c="dimmed" fw={500} mb={4}>ПРИЧИНА ОТКЛАДЫВАНИЯ</Text>
              <Text size="sm" style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', maxHeight: 200, overflowY: 'auto' }}>
                {group.postponeReason}
              </Text>
            </Paper>
          )}

          {/* Action buttons */}
          <Group gap="xs" wrap="wrap">
            {isNoProduct && permCatalogFast && (
              <Tooltip
                label={
                  missingForQuick.length
                    ? `Сначала заполните в метаданных: ${missingForQuick.join(', ')}`
                    : 'Быстрая каталогизация — создать новый продукт из метаданных'
                }
              >
                <Button
                  size="sm"
                  color="teal"
                  variant="light"
                  leftSection={<IconWand size={14} />}
                  loading={actionLoading === 'catalog-fast'}
                  disabled={missingForQuick.length > 0}
                  onClick={handleCatalogFast}
                >
                  Быстрый каталог
                </Button>
              </Tooltip>
            )}
            {isNoProduct && permPossibleProducts && (
              <Button
                size="sm"
                color="blue"
                variant="light"
                leftSection={<IconSearch size={14} />}
                onClick={openPossible}
              >
                Возможные продукты
              </Button>
            )}

            {isNoRights && permCorrectRights && (
              <>
                <Tooltip label="Создать права в каталоге из заполненных метаправ и перевести группу в статус 88">
                  <Button
                    size="sm"
                    color="green"
                    variant="filled"
                    leftSection={<IconShieldCheck size={14} />}
                    loading={actionLoading === 'create-rights'}
                    onClick={openCreateRights}
                  >
                    Создать права
                  </Button>
                </Tooltip>
                <Tooltip label="Найти права у похожего продукта в каталоге">
                  <Button
                    size="sm"
                    color="grape"
                    variant="light"
                    leftSection={<IconSearch size={14} />}
                    onClick={openSearchRights}
                  >
                    Найти права
                  </Button>
                </Tooltip>
              </>
            )}

            {permExport && (
              <Button
                size="sm"
                color="green"
                variant="light"
                leftSection={<IconDownload size={14} />}
                onClick={handleExport}
              >
                Экспорт
              </Button>
            )}

            {(isNoProduct || isNoRights) && permSendBackoffice && (
              <Tooltip label="Эскалация в бэк-офис">
                <Button
                  size="sm"
                  color="gray"
                  variant="light"
                  leftSection={<IconBuildingWarehouse size={14} />}
                  onClick={openBo}
                >
                  Бэк-офис
                </Button>
              </Tooltip>
            )}
            {(isNoProduct || isNoRights) && permPostpone && (
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
            {isPostponed && permReturn && (
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
            {statusAllowsUngroup && permUngroup && (
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
          <SuspensesTab groupId={groupId} businessStatus={group.businessStatus} />
        </Tabs.Panel>

        <Tabs.Panel value="metadata" pt="md">
          <Paper withBorder radius="md" p="md">
            <MetadataTab
              groupId={groupId}
              metadata={metadata ?? null}
              readOnly={group.catalogProductId != null || !permCatalogFast}
              linkedToProduct={group.catalogProductId != null}
            />
          </Paper>
        </Tabs.Panel>

        {isNoRights && (
          <Tabs.Panel value="rights" pt="md">
            <Paper withBorder radius="md" p="md">
              <MetaRightsTab groupId={groupId} metaRights={metaRights ?? null} readOnly={!permCorrectRights} />
            </Paper>
          </Tabs.Panel>
        )}
      </Tabs>

      {/* Possible Products Modal */}
      <PossibleProductsModal
        opened={possibleOpen}
        onClose={closePossible}
        groupId={groupId}
        onLink={refreshGroup}
      />

      {/* Search Rights Modal */}
      {group && (
        <SearchRightsCatalogModal
          opened={searchRightsOpen}
          onClose={closeSearchRights}
          group={group}
          onApplied={refreshGroup}
          searchRights={(params) => searchCatalogRights(groupId, params)}
          copyRights={async (rightsId) => {
            await copyRightsToProduct(groupId, rightsId);
          }}
        />
      )}

      {/* Create Rights Confirm Modal */}
      <Modal opened={createRightsOpen} onClose={closeCreateRights} title="Подтверждение создания прав" centered radius="md" size="md">
        <Stack gap="md">
          <Text size="sm" c="dimmed">
            В каталоге будет создана запись прав для продукта{' '}
            <strong>{group?.catalogProduct?.productName ?? `#${group?.catalogProductId}`}</strong>.
            Эти права будут применяться ко всем будущим суспенсам с этим продуктом автоматически.
          </Text>
          {(() => {
            const r = metaRights;
            const rows: { label: string; value: string | number | null | undefined }[] = [
              { label: 'Компания-отправитель', value: r?.senderCompany?.shortName ?? r?.senderCompanyId },
              { label: 'Компания-получатель', value: r?.receiverCompany?.shortName ?? r?.receiverCompanyId },
              { label: 'Территория', value: r?.territoryCode },
              { label: 'Номер договора', value: r?.docNumber },
              { label: 'Период', value: r?.docStart && r?.docEnd ? `${r.docStart} — ${r.docEnd}` : r?.docStart ?? r?.docEnd },
              { label: 'Доля', value: r?.share != null ? `${r.share}%` : null },
            ];
            return (
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
            );
          })()}
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={closeCreateRights}>Отмена</Button>
            <Button color="green" loading={actionLoading === 'create-rights'} onClick={handleCreateRightsConfirm}>
              Создать права
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* Catalog Fast Confirm Modal */}
      <Modal opened={catalogFastOpen} onClose={closeCatalogFast} title="Подтверждение быстрой каталогизации" centered radius="md" size="md">
        <Stack gap="md">
          <Text size="sm" c="dimmed">
            Будет создан новый продукт в каталоге со следующими данными из метаданных группы:
          </Text>
          {(() => {
            const m = metadata ?? group?.groupMetaData;
            const rows: { label: string; value: string | null | undefined }[] = [
              { label: 'Название', value: m?.title },
              { label: 'Исполнитель', value: m?.artist },
              { label: 'ISRC', value: m?.isrc },
              { label: 'Баркод', value: m?.barcode },
              { label: 'Каталожный №', value: m?.catalogNumber },
              { label: 'Жанр', value: m?.genre },
              { label: 'Дата выпуска', value: m?.releaseDate },
              { label: 'Описание', value: m?.description },
            ];
            return (
              <Paper withBorder radius="md" p="sm">
                <Stack gap={6}>
                  {rows.map(({ label, value }) => value ? (
                    <Group key={label} gap="xs" align="flex-start">
                      <Text size="xs" c="dimmed" w={120} style={{ flexShrink: 0 }}>{label}</Text>
                      <Text size="sm" fw={500}>{value}</Text>
                    </Group>
                  ) : null)}
                </Stack>
              </Paper>
            );
          })()}
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={closeCatalogFast}>Отмена</Button>
            <Button color="teal" loading={actionLoading === 'catalog-fast'} onClick={handleCatalogFastConfirm}>
              Создать продукт
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* Back Office Modal */}
      <Modal opened={boOpen} onClose={() => { closeBo(); setBoCommentError(null); }} title="Отправить в бэк-офис" centered radius="md">
        <Stack gap="md">
          <Textarea
            label="Комментарий для бэк-офиса"
            placeholder="Опишите причину отправки, что не удалось найти, что нужно проверить..."
            value={boComment}
            onChange={(e) => { setBoComment(e.target.value); if (e.target.value.trim()) setBoCommentError(null); }}
            rows={4}
            maxLength={2000}
            description={boCommentError ? undefined : `${boComment.length} / 2000`}
            error={boCommentError}
            required
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
            maxLength={500}
            description={`${postponeReason.length} / 500`}
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
