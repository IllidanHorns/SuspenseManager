import { useState, useMemo } from 'react';
import {
  Stack, Box, Title, Text, Tabs, Paper, Table, ScrollArea,
  Group, Button, Modal, TextInput, NumberInput, Select, ActionIcon,
  Center, Loader, Alert, Pagination, SimpleGrid, Badge, List,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { useDisclosure } from '@mantine/hooks';
import { notifications } from '@mantine/notifications';
import { IconAlertCircle, IconAlertTriangle, IconInfoCircle, IconPencil, IconPlus, IconSearch, IconX } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getCatalogProducts, createCatalogProduct, updateCatalogProduct,
  getCatalogProductTypes,
  getCatalogRights, createCatalogRights, updateCatalogRights,
  getCatalogCompanies, createCompany, updateCompany,
  getCatalogTerritories, createTerritory, updateTerritory,
} from '../api/catalog';
import { PageSizeSelect } from '../components/common/PageSizeSelect';
import { ResizableTh } from '../components/common/ResizableTh';
import { CollapsibleFilters } from '../components/common/CollapsibleFilters';
import { useDefaultPageSize } from '../hooks/useDefaultPageSize';
import { fmtDate } from '../utils/format';
import {
  CATALOG_RIGHTS_MANDATORY_FIELDS_TEXT,
  getMissingCatalogRightsIdentityFields,
} from '../utils/catalogRightsIdentity';
import {
  CATALOG_PRODUCT_MANDATORY_FIELDS_TEXT,
  getMissingCatalogProductIdentityFields,
} from '../utils/requiredProductIdentity';
import {
  DbMax,
  bicOptional,
  combine,
  dateIsoOptional,
  innOptional,
  maxLen,
  optMaxLen,
  phoneOptional,
  required,
  sharePercent,
} from '../utils/fieldValidation';
import { PhoneRequirementsModal } from '../components/common/PhoneRequirementsModal';
import { CompanyIdentifiersModal } from '../components/common/CompanyIdentifiersModal';
import type {
  CatalogProduct, CatalogProductRights, Company, Territory,
  CreateCatalogProductDto, UpdateCatalogProductDto,
  CreateCatalogProductRightsDto, UpdateCatalogProductRightsDto,
  CreateCompanyDto, UpdateCompanyDto,
  CreateTerritoryDto, UpdateTerritoryDto,
} from '../types';

// ── Products Tab ──────────────────────────────────────────────────────────────

function ProductsTab() {
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useDefaultPageSize();
  const [editItem, setEditItem] = useState<CatalogProduct | null>(null);
  const [opened, { open, close }] = useDisclosure(false);
  const [missingFieldsProduct, setMissingFieldsProduct] = useState<CatalogProduct | null>(null);

  // Pending filter inputs
  const [pName, setPName] = useState('');
  const [pArtist, setPArtist] = useState('');
  const [pIsrc, setPIsrc] = useState('');
  const [pBarcode, setPBarcode] = useState('');
  const [pGenre, setPGenre] = useState('');
  const [pCatalogNumber, setPCatalogNumber] = useState('');

  // Applied filters (sent to server)
  const [applied, setApplied] = useState<Record<string, string>>({});
  const [incompleteOnly, setIncompleteOnly] = useState(false);

  const buildFilters = () => {
    const f: Record<string, string> = {};
    if (pName.trim()) f['ProductName_contains'] = pName.trim();
    if (pArtist.trim()) f['Artist_contains'] = pArtist.trim();
    if (pIsrc.trim()) f['Isrc_contains'] = pIsrc.trim();
    if (pBarcode.trim()) f['Barcode_contains'] = pBarcode.trim();
    if (pGenre.trim()) f['Genre_contains'] = pGenre.trim();
    if (pCatalogNumber.trim()) f['CatalogNumber_contains'] = pCatalogNumber.trim();
    return f;
  };

  const applyFilters = () => { setApplied(buildFilters()); setPage(1); };
  const resetFilters = () => {
    setPName(''); setPArtist(''); setPIsrc(''); setPBarcode(''); setPGenre(''); setPCatalogNumber('');
    setApplied({}); setIncompleteOnly(false); setPage(1);
  };

  const hasActive = Object.keys(applied).length > 0 || incompleteOnly;
  const handleEnter = (e: React.KeyboardEvent) => { if (e.key === 'Enter') applyFilters(); };

  const form = useForm<CreateCatalogProductDto>({
    initialValues: {
      productName: '', artist: '', isrc: '', barcode: '',
      catalogNumber: '', genre: '', releaseDate: null, productTypeId: null,
    },
    validate: {
      productName: optMaxLen(DbMax.catalogProduct.productName, 'Название'),
      artist: optMaxLen(DbMax.catalogProduct.artist, 'Артист'),
      isrc: optMaxLen(DbMax.catalogProduct.isrc, 'ISRC'),
      barcode: optMaxLen(DbMax.catalogProduct.barcode, 'Штрихкод'),
      catalogNumber: optMaxLen(DbMax.catalogProduct.catalogNumber, 'Каталожный номер'),
      genre: optMaxLen(DbMax.catalogProduct.genre, 'Жанр'),
      releaseDate: (v) => dateIsoOptional('Дата релиза')(v as string | null | undefined),
    },
  });

  const { data: productTypes } = useQuery({
    queryKey: ['catalog-product-types'],
    queryFn: getCatalogProductTypes,
    staleTime: 5 * 60 * 1000,
  });

  const productTypeOptions = (productTypes ?? []).map((t) => ({
    value: String(t.id),
    label: `${t.code} — ${t.description}`,
  }));

  const { data, isLoading, error } = useQuery({
    queryKey: ['catalog-products', page, pageSize, applied, incompleteOnly],
    queryFn: () => {
      const f: Record<string, string> = { ...applied };
      if (incompleteOnly) f.IdentityIncomplete = 'true';
      return getCatalogProducts({
        pageNumber: page,
        pageSize,
        Filters: Object.keys(f).length ? f : undefined,
      });
    },
  });

  const createMutation = useMutation({
    mutationFn: (dto: CreateCatalogProductDto) => createCatalogProduct(dto),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['catalog-products'] }); notifications.show({ message: 'Продукт создан', color: 'green' }); close(); },
    onError: (e: Error) => notifications.show({ message: e.message, color: 'red' }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, dto }: { id: number; dto: UpdateCatalogProductDto }) => updateCatalogProduct(id, dto),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['catalog-products'] }); notifications.show({ message: 'Продукт обновлён', color: 'green' }); close(); },
    onError: (e: Error) => notifications.show({ message: e.message, color: 'red' }),
  });

  const handleOpen = (item?: CatalogProduct) => {
    if (item) {
      setEditItem(item);
      form.setValues({
        productName: item.productName ?? '', artist: item.artist ?? '',
        isrc: item.isrc ?? '', barcode: item.barcode ?? '',
        catalogNumber: item.catalogNumber ?? '', genre: item.genre ?? '',
        releaseDate: item.releaseDate ?? null, productTypeId: item.productTypeId ?? null,
      });
    } else {
      setEditItem(null);
      form.reset();
    }
    open();
  };

  const handleSubmit = form.onSubmit((values) => {
    if (editItem) {
      const effective: CatalogProduct = {
        ...editItem,
        productName: values.productName?.trim() || null,
        artist: values.artist?.trim() || null,
        isrc: values.isrc?.trim() || null,
        barcode: values.barcode?.trim() || null,
        catalogNumber: values.catalogNumber?.trim() || null,
      };
      const missing = getMissingCatalogProductIdentityFields(effective);
      if (missing.length > 0) {
        notifications.show({
          title: 'Нельзя сохранить',
          message:
            `Обязательные поля не могут быть пустыми. Не заполнено: ${missing.join(', ')}. ` +
            'Если вы очистили поле и нажали «Сохранить», сервер не меняет это поле на пустое — остаётся старое значение.',
          color: 'orange',
        });
        return;
      }
    } else {
      const draft: CatalogProduct = {
        id: 0,
        isrc: values.isrc?.trim() || null,
        barcode: values.barcode?.trim() || null,
        catalogNumber: values.catalogNumber?.trim() || null,
        productName: values.productName?.trim() || null,
        artist: values.artist?.trim() || null,
        genre: values.genre?.trim() || null,
        duration: null,
        releaseDate: values.releaseDate || null,
        productTypeId: values.productTypeId ?? null,
        createTime: '',
        archiveLevel: 0,
      };
      const missing = getMissingCatalogProductIdentityFields(draft);
      if (missing.length > 0) {
        notifications.show({
          title: 'Нельзя сохранить',
          message: `Заполните обязательные поля. Не заполнено: ${missing.join(', ')}.`,
          color: 'orange',
        });
        return;
      }
    }

    const dto = {
      ...values,
      productName: values.productName || null,
      artist: values.artist || null,
      isrc: values.isrc || null,
      barcode: values.barcode || null,
      catalogNumber: values.catalogNumber || null,
      genre: values.genre || null,
      releaseDate: values.releaseDate || null,
    };
    if (editItem) updateMutation.mutate({ id: editItem.id, dto });
    else createMutation.mutate(dto);
  });

  const handlePageSize = (v: number) => { setPageSize(v); setPage(1); };
  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <>
      <Box mx="sm" mb="sm">
        <CollapsibleFilters activeCount={Object.keys(applied).length + (incompleteOnly ? 1 : 0)}>
          <Paper withBorder radius="md" p="sm">
            <Stack gap="xs">
              <Group justify="space-between" align="flex-end" wrap="wrap" gap="sm">
                <Group gap="sm" align="flex-end" wrap="wrap">
                  <TextInput size="xs" label="Название" placeholder="Поиск..." style={{ width: 160 }}
                    value={pName} onChange={(e) => setPName(e.target.value)} onKeyDown={handleEnter} />
                  <TextInput size="xs" label="Артист" placeholder="Поиск..." style={{ width: 160 }}
                    value={pArtist} onChange={(e) => setPArtist(e.target.value)} onKeyDown={handleEnter} />
                  <TextInput size="xs" label="ISRC" placeholder="Поиск..." style={{ width: 150 }}
                    value={pIsrc} onChange={(e) => setPIsrc(e.target.value)} onKeyDown={handleEnter} />
                  <TextInput size="xs" label="Баркод" placeholder="Поиск..." style={{ width: 140 }}
                    value={pBarcode} onChange={(e) => setPBarcode(e.target.value)} onKeyDown={handleEnter} />
                  <TextInput size="xs" label="Жанр" placeholder="Поиск..." style={{ width: 120 }}
                    value={pGenre} onChange={(e) => setPGenre(e.target.value)} onKeyDown={handleEnter} />
                  <TextInput size="xs" label="Кат. номер" placeholder="Поиск..." style={{ width: 130 }}
                    value={pCatalogNumber} onChange={(e) => setPCatalogNumber(e.target.value)} onKeyDown={handleEnter} />
                  <Group gap="xs" style={{ alignSelf: 'flex-end' }} wrap="nowrap">
                    <Button size="xs" leftSection={<IconSearch size={12} />} onClick={applyFilters}>Найти</Button>
                    {hasActive && (
                      <Button size="xs" variant="subtle" color="gray" leftSection={<IconX size={12} />} onClick={resetFilters}>Сбросить</Button>
                    )}
                    <Button
                      size="xs"
                      variant={incompleteOnly ? 'filled' : 'light'}
                      color="orange"
                      leftSection={<IconAlertTriangle size={14} />}
                      style={{ flexShrink: 0 }}
                      onClick={() => {
                        setIncompleteOnly((v) => !v);
                        setPage(1);
                      }}
                    >
                      {incompleteOnly ? 'Все продукты' : 'Неполные продукты'}
                    </Button>
                  </Group>
                </Group>
                <Button
                  leftSection={<IconPlus size={14} />}
                  size="xs"
                  style={{ flexShrink: 0 }}
                  onClick={() => handleOpen()}
                >
                  Добавить продукт
                </Button>
              </Group>
              <Text size="xs" c="dimmed">
                Обязательные поля данных продукта: <Text span fw={600} c="dark">{CATALOG_PRODUCT_MANDATORY_FIELDS_TEXT}</Text>.
                Пустые или только пробелы считаются незаполненными.
              </Text>
            </Stack>
          </Paper>
        </CollapsibleFilters>
      </Box>

      {isLoading ? (
        <Center py="xl"><Loader size="sm" /></Center>
      ) : error ? (
        <Alert icon={<IconAlertCircle size={16} />} color="red" m="md">
          {error instanceof Error ? error.message : 'Ошибка загрузки'}
        </Alert>
      ) : !data?.items.length ? (
        <Center py="xl"><Text c="dimmed">Продукты не найдены</Text></Center>
      ) : (
        <ScrollArea>
          <Table striped highlightOnHover style={{ minWidth: 920 }}>
            <Table.Thead>
              <Table.Tr>
                <ResizableTh>ID</ResizableTh>
                <ResizableTh>Название</ResizableTh>
                <ResizableTh>Артист</ResizableTh>
                <ResizableTh>ISRC</ResizableTh>
                <ResizableTh>Штрихкод</ResizableTh>
                <ResizableTh>Кат. номер</ResizableTh>
                <ResizableTh>Жанр</ResizableTh>
                <ResizableTh>Дата релиза</ResizableTh>
                <ResizableTh style={{ width: 88 }} />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {data.items.map((p) => {
                const missing = getMissingCatalogProductIdentityFields(p);
                return (
                <Table.Tr key={p.id}>
                  <Table.Td><Text size="sm" c="dimmed">#{p.id}</Text></Table.Td>
                  <Table.Td><Text size="sm" fw={500}>{p.productName ?? '—'}</Text></Table.Td>
                  <Table.Td><Text size="sm">{p.artist ?? '—'}</Text></Table.Td>
                  <Table.Td><Text size="sm" ff="monospace">{p.isrc ?? '—'}</Text></Table.Td>
                  <Table.Td><Text size="sm" ff="monospace">{p.barcode ?? '—'}</Text></Table.Td>
                  <Table.Td><Text size="sm">{p.catalogNumber ?? '—'}</Text></Table.Td>
                  <Table.Td><Text size="sm">{p.genre ?? '—'}</Text></Table.Td>
                  <Table.Td><Text size="sm">{fmtDate(p.releaseDate)}</Text></Table.Td>
                  <Table.Td style={{ whiteSpace: 'nowrap' }}>
                    <Group gap={4} wrap="nowrap" justify="flex-end">
                      {missing.length > 0 ? (
                        <ActionIcon
                          variant="light"
                          color="orange"
                          size="sm"
                          aria-label="Какие обязательные поля не заполнены"
                          onClick={() => setMissingFieldsProduct(p)}
                        >
                          <IconAlertCircle size={18} />
                        </ActionIcon>
                      ) : null}
                      <ActionIcon variant="subtle" size="sm" aria-label="Редактировать" onClick={() => handleOpen(p)}>
                        <IconPencil size={14} />
                      </ActionIcon>
                    </Group>
                  </Table.Td>
                </Table.Tr>
                );
              })}
            </Table.Tbody>
          </Table>
        </ScrollArea>
      )}

      {!isLoading && (data?.items.length ?? 0) > 0 && (
        <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
          <Text size="sm" c="dimmed">Всего: {data!.totalCount}</Text>
          <Group gap="sm">
            <PageSizeSelect value={pageSize} onChange={handlePageSize} />
            <Pagination value={page} onChange={setPage} total={Math.max(1, data!.totalPages)} size="sm" />
          </Group>
        </Group>
      )}

      <Modal
        opened={missingFieldsProduct !== null}
        onClose={() => setMissingFieldsProduct(null)}
        title={
          missingFieldsProduct
            ? `Незаполненные поля · продукт #${missingFieldsProduct.id}`
            : ''
        }
        size="sm"
        radius="md"
      >
        {missingFieldsProduct && (
          <Stack gap="md">
            <Text size="sm" c="dimmed">
              {missingFieldsProduct.productName?.trim()
                ? missingFieldsProduct.productName
                : 'Название не указано'}
            </Text>
            <div>
              <Text size="sm" fw={600} mb="xs">
                Сейчас не заполнено:
              </Text>
              <List size="sm" spacing="xs" withPadding listStyleType="disc">
                {getMissingCatalogProductIdentityFields(missingFieldsProduct).map((m) => (
                  <List.Item key={m}>{m}</List.Item>
                ))}
              </List>
            </div>
            <Alert color="gray" variant="light" title="Обязательные поля данных продукта">
              Должны быть заданы: {CATALOG_PRODUCT_MANDATORY_FIELDS_TEXT}. Пустая строка и пробелы не подходят.
            </Alert>
            <Group justify="flex-end">
              <Button variant="default" size="xs" onClick={() => setMissingFieldsProduct(null)}>
                Закрыть
              </Button>
              <Button
                size="xs"
                leftSection={<IconPencil size={14} />}
                onClick={() => {
                  const row = missingFieldsProduct;
                  setMissingFieldsProduct(null);
                  handleOpen(row);
                }}
              >
                Редактировать продукт
              </Button>
            </Group>
          </Stack>
        )}
      </Modal>

      <Modal opened={opened} onClose={close} title={editItem ? `Редактировать продукт #${editItem.id}` : 'Добавить продукт'} size="lg">
        <form onSubmit={handleSubmit}>
          <SimpleGrid cols={2} spacing="sm">
            <TextInput label="Название" {...form.getInputProps('productName')} />
            <TextInput label="Артист" {...form.getInputProps('artist')} />
            <TextInput label="ISRC" {...form.getInputProps('isrc')} />
            <TextInput label="Штрихкод" {...form.getInputProps('barcode')} />
            <TextInput label="Каталожный номер" {...form.getInputProps('catalogNumber')} />
            <TextInput label="Жанр" {...form.getInputProps('genre')} />
            <TextInput label="Дата релиза" placeholder="YYYY-MM-DD" {...form.getInputProps('releaseDate')} />
            <Select
              label="Тип продукта"
              placeholder="Выберите тип"
              data={productTypeOptions}
              clearable
              value={form.values.productTypeId != null ? String(form.values.productTypeId) : null}
              onChange={(val) => form.setFieldValue('productTypeId', val ? Number(val) : null)}
              error={form.errors.productTypeId}
            />
          </SimpleGrid>
          <Group justify="flex-end" mt="md">
            <Button variant="subtle" onClick={close} disabled={isPending}>Отмена</Button>
            <Button type="submit" loading={isPending}>{editItem ? 'Сохранить' : 'Создать'}</Button>
          </Group>
        </form>
      </Modal>
    </>
  );
}

// ── Rights Tab ────────────────────────────────────────────────────────────────

function RightsTab() {
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useDefaultPageSize();
  const [editItem, setEditItem] = useState<CatalogProductRights | null>(null);
  const [opened, { open, close }] = useDisclosure(false);
  const [missingRightsItem, setMissingRightsItem] = useState<CatalogProductRights | null>(null);

  // Pending filter inputs
  const [pProductId, setPProductId] = useState('');
  const [pSender, setPSender] = useState('');
  const [pReceiver, setPReceiver] = useState('');
  const [pTerritory, setPTerritory] = useState('');
  const [pDocNumber, setPDocNumber] = useState('');

  const [appliedProductId, setAppliedProductId] = useState<number | undefined>(undefined);
  const [appliedFilters, setAppliedFilters] = useState<Record<string, string>>({});
  const [incompleteRightsOnly, setIncompleteRightsOnly] = useState(false);

  const buildApplied = () => {
    const f: Record<string, string> = {};
    if (pSender.trim()) f['CompanySender_contains'] = pSender.trim();
    if (pReceiver.trim()) f['CompanyReceiver_contains'] = pReceiver.trim();
    if (pTerritory.trim()) f['TerritoryCode_contains'] = pTerritory.trim();
    if (pDocNumber.trim()) f['DocNumber_contains'] = pDocNumber.trim();
    return f;
  };

  const applyFilters = () => {
    setAppliedProductId(pProductId ? Number(pProductId) : undefined);
    setAppliedFilters(buildApplied());
    setPage(1);
  };

  const resetFilters = () => {
    setPProductId(''); setPSender(''); setPReceiver(''); setPTerritory(''); setPDocNumber('');
    setAppliedProductId(undefined); setAppliedFilters({}); setIncompleteRightsOnly(false); setPage(1);
  };

  const hasActive = !!pProductId || !!pSender || !!pReceiver || !!pTerritory || !!pDocNumber || incompleteRightsOnly;
  const rightsFilterActiveCount = (appliedProductId !== undefined && appliedProductId !== null && !Number.isNaN(appliedProductId) ? 1 : 0)
    + Object.keys(appliedFilters).length + (incompleteRightsOnly ? 1 : 0);
  const handleEnter = (e: React.KeyboardEvent) => { if (e.key === 'Enter') applyFilters(); };

  const createForm = useForm<CreateCatalogProductRightsDto>({
    initialValues: { catalogProductId: 0, docNumber: '', companySenderId: 0, companyReceiverId: 0, share: 100, territoryId: 0, docStart: '', docEnd: '' },
    validate: {
      catalogProductId: (v) => (!v ? 'Укажите ID продукта' : null),
      companySenderId: (v) => (!v ? 'Выберите отправителя' : null),
      companyReceiverId: (v) => (!v ? 'Выберите получателя' : null),
      territoryId: (v) => (!v ? 'Выберите территорию' : null),
      docNumber: optMaxLen(DbMax.catalogRights.docNumber, 'Номер договора'),
      docStart: combine(required('Укажите дату начала'), dateIsoOptional('Начало')),
      docEnd: (v, values) => {
        const base = combine(required('Укажите дату окончания'), dateIsoOptional('Окончание'))(v);
        if (base) return base;
        if (values.docStart && v && v <= values.docStart) return 'Дата окончания должна быть позже даты начала';
        return null;
      },
      share: (v) => {
        const base = sharePercent('Доля')(v);
        if (base) return base;
        if (v < 1) return 'Доля должна быть от 1 до 100%';
        return null;
      },
    },
  });

  const editForm = useForm<UpdateCatalogProductRightsDto>({
    initialValues: { docNumber: null, companySenderId: null, companyReceiverId: null, share: null, territoryId: null, docStart: null, docEnd: null },
    validate: {
      docNumber: (v) => optMaxLen(DbMax.catalogRights.docNumber, 'Номер договора')(v ?? ''),
      docStart: (v) => dateIsoOptional('Начало действия')(v ?? ''),
      docEnd: (v, values) => {
        const base = dateIsoOptional('Конец действия')(v ?? '');
        if (base) return base;
        const start = values.docStart ?? '';
        if (start && v && v <= start) return 'Дата окончания должна быть позже даты начала';
        return null;
      },
      share: (v) => {
        if (v === null || v === undefined) return null;
        const base = sharePercent('Доля')(v);
        if (base) return base;
        if (v < 1) return 'Доля должна быть от 1 до 100%';
        return null;
      },
    },
  });

  const { data: companiesData } = useQuery({ queryKey: ['catalog-companies'], queryFn: () => getCatalogCompanies() });
  const { data: territoriesData } = useQuery({ queryKey: ['catalog-territories'], queryFn: () => getCatalogTerritories() });

  const { data, isLoading, error } = useQuery({
    queryKey: ['catalog-rights', page, pageSize, appliedProductId, appliedFilters, incompleteRightsOnly],
    queryFn: () => {
      const f: Record<string, string> = { ...appliedFilters };
      if (incompleteRightsOnly) f.RightsIncomplete = 'true';
      return getCatalogRights({
        pageNumber: page,
        pageSize,
        productId: appliedProductId,
        Filters: Object.keys(f).length ? f : undefined,
      });
    },
  });

  const companies = companiesData?.items ?? [];
  const territories = territoriesData?.items ?? [];
  const companyOptions = companies.map((c) => ({ value: String(c.id), label: `${c.shortName} (${c.companyCode ?? c.legalName})` }));
  const territoryOptions = territories.map((t) => ({ value: String(t.id), label: `${t.territoryCode}${t.territoryName ? ' — ' + t.territoryName : ''}` }));

  const createMutation = useMutation({
    mutationFn: (dto: CreateCatalogProductRightsDto) => createCatalogRights(dto),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['catalog-rights'] }); notifications.show({ message: 'Права созданы', color: 'green' }); close(); },
    onError: (e: Error) => notifications.show({ message: e.message, color: 'red' }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, dto }: { id: number; dto: UpdateCatalogProductRightsDto }) => updateCatalogRights(id, dto),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['catalog-rights'] }); notifications.show({ message: 'Права обновлены', color: 'green' }); close(); },
    onError: (e: Error) => notifications.show({ message: e.message, color: 'red' }),
  });

  const handleOpenCreate = () => { setEditItem(null); createForm.reset(); open(); };
  const handleOpenEdit = (item: CatalogProductRights) => {
    setEditItem(item);
    editForm.setValues({ docNumber: item.docNumber, companySenderId: item.companySenderId, companyReceiverId: item.companyReceiverId, share: item.share, territoryId: item.territoryId, docStart: item.docStart, docEnd: item.docEnd });
    open();
  };

  const handlePageSize = (v: number) => { setPageSize(v); setPage(1); };
  const isPending = createMutation.isPending || updateMutation.isPending;

  const submitRightsEdit = editForm.onSubmit((values) => {
    if (!editItem) return;
    const effectiveTid = values.territoryId ?? editItem.territoryId;
    const terr = territories.find((t) => t.id === effectiveTid);
    const effective: CatalogProductRights = {
      ...editItem,
      companySenderId: values.companySenderId ?? editItem.companySenderId,
      companyReceiverId: values.companyReceiverId ?? editItem.companyReceiverId,
      territoryId: effectiveTid,
      territoryCode:
        values.territoryId != null && terr ? terr.territoryCode : editItem.territoryCode,
      territoryDesc:
        values.territoryId != null && terr ? terr.territoryName ?? '' : editItem.territoryDesc,
      share: values.share ?? editItem.share,
      docStart: values.docStart ?? editItem.docStart,
      docEnd: values.docEnd ?? editItem.docEnd,
      docNumber: values.docNumber !== undefined ? values.docNumber : editItem.docNumber,
    };
    const missing = getMissingCatalogRightsIdentityFields(effective);
    if (missing.length > 0) {
      notifications.show({
        title: 'Нельзя сохранить',
        message:
          `После сохранения запись была бы неполной. Не хватает: ${missing.join(', ')}. ` +
          'Пустое поле в форме не стирает значение в базе — верните данные или выберите значение.',
        color: 'orange',
      });
      return;
    }
    updateMutation.mutate({ id: editItem.id, dto: values });
  });

  return (
    <>
      <Box mx="sm" mb="sm">
        <CollapsibleFilters activeCount={rightsFilterActiveCount}>
          <Paper withBorder radius="md" p="sm">
            <Stack gap="xs">
              <Group justify="space-between" align="flex-end" wrap="wrap" gap="sm">
                <Group gap="sm" align="flex-end" wrap="wrap">
                  <TextInput size="xs" label="ID продукта" placeholder="Точное" style={{ width: 110 }}
                    value={pProductId} onChange={(e) => setPProductId(e.target.value)} onKeyDown={handleEnter} />
                  <TextInput size="xs" label="Отправитель" placeholder="Поиск..." style={{ width: 160 }}
                    value={pSender} onChange={(e) => setPSender(e.target.value)} onKeyDown={handleEnter} />
                  <TextInput size="xs" label="Получатель" placeholder="Поиск..." style={{ width: 160 }}
                    value={pReceiver} onChange={(e) => setPReceiver(e.target.value)} onKeyDown={handleEnter} />
                  <TextInput size="xs" label="Территория" placeholder="RU, US..." style={{ width: 110 }}
                    value={pTerritory} onChange={(e) => setPTerritory(e.target.value)} onKeyDown={handleEnter} />
                  <TextInput size="xs" label="№ договора" placeholder="Поиск..." style={{ width: 140 }}
                    value={pDocNumber} onChange={(e) => setPDocNumber(e.target.value)} onKeyDown={handleEnter} />
                  <Group gap="xs" style={{ alignSelf: 'flex-end' }} wrap="nowrap">
                    <Button size="xs" leftSection={<IconSearch size={12} />} onClick={applyFilters}>Найти</Button>
                    {hasActive && (
                      <Button size="xs" variant="subtle" color="gray" leftSection={<IconX size={12} />} onClick={resetFilters}>Сбросить</Button>
                    )}
                    <Button
                      size="xs"
                      variant={incompleteRightsOnly ? 'filled' : 'light'}
                      color="orange"
                      leftSection={<IconAlertTriangle size={14} />}
                      style={{ flexShrink: 0 }}
                      onClick={() => {
                        setIncompleteRightsOnly((v) => !v);
                        setPage(1);
                      }}
                    >
                      {incompleteRightsOnly ? 'Все права' : 'Неполные права'}
                    </Button>
                  </Group>
                </Group>
                <Button
                  leftSection={<IconPlus size={14} />}
                  size="xs"
                  style={{ flexShrink: 0 }}
                  onClick={handleOpenCreate}
                >
                  Добавить права
                </Button>
              </Group>
              <Text size="xs" c="dimmed">
                Обязательные поля записи прав: <Text span fw={600} c="dark">{CATALOG_RIGHTS_MANDATORY_FIELDS_TEXT}</Text>.
                Код территории не может быть пустым или из одних пробелов.
              </Text>
            </Stack>
          </Paper>
        </CollapsibleFilters>
      </Box>

      {isLoading ? (
        <Center py="xl"><Loader size="sm" /></Center>
      ) : error ? (
        <Alert icon={<IconAlertCircle size={16} />} color="red" m="md">
          {error instanceof Error ? error.message : 'Ошибка загрузки'}
        </Alert>
      ) : !data?.items.length ? (
        <Center py="xl"><Text c="dimmed">Права не найдены</Text></Center>
      ) : (
        <ScrollArea>
          <Table striped highlightOnHover style={{ minWidth: 980 }}>
            <Table.Thead>
              <Table.Tr>
                <ResizableTh>ID</ResizableTh>
                <ResizableTh>Продукт ID</ResizableTh>
                <ResizableTh>Отправитель</ResizableTh>
                <ResizableTh>Получатель</ResizableTh>
                <ResizableTh>Доля %</ResizableTh>
                <ResizableTh>Территория</ResizableTh>
                <ResizableTh>С</ResizableTh>
                <ResizableTh>По</ResizableTh>
                <ResizableTh>Договор</ResizableTh>
                <ResizableTh style={{ width: 88 }} />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {data.items.map((r) => {
                const missing = getMissingCatalogRightsIdentityFields(r);
                return (
                <Table.Tr key={r.id}>
                  <Table.Td><Text size="sm" c="dimmed">#{r.id}</Text></Table.Td>
                  <Table.Td><Text size="sm">#{r.catalogProductId}</Text></Table.Td>
                  <Table.Td><Text size="sm">{r.companySender}</Text></Table.Td>
                  <Table.Td><Text size="sm">{r.companyReceiver}</Text></Table.Td>
                  <Table.Td><Badge variant="light" size="sm">{r.share}%</Badge></Table.Td>
                  <Table.Td><Text size="sm">{r.territoryCode}</Text></Table.Td>
                  <Table.Td><Text size="sm">{fmtDate(r.docStart)}</Text></Table.Td>
                  <Table.Td><Text size="sm">{fmtDate(r.docEnd)}</Text></Table.Td>
                  <Table.Td><Text size="sm" c="dimmed">{r.docNumber ?? '—'}</Text></Table.Td>
                  <Table.Td style={{ whiteSpace: 'nowrap' }}>
                    <Group gap={4} wrap="nowrap" justify="flex-end">
                      {missing.length > 0 ? (
                        <ActionIcon
                          variant="light"
                          color="orange"
                          size="sm"
                          aria-label="Какие обязательные поля не заполнены"
                          onClick={() => setMissingRightsItem(r)}
                        >
                          <IconAlertCircle size={18} />
                        </ActionIcon>
                      ) : null}
                      <ActionIcon variant="subtle" size="sm" aria-label="Редактировать" onClick={() => handleOpenEdit(r)}>
                        <IconPencil size={14} />
                      </ActionIcon>
                    </Group>
                  </Table.Td>
                </Table.Tr>
                );
              })}
            </Table.Tbody>
          </Table>
        </ScrollArea>
      )}

      {!isLoading && (data?.items.length ?? 0) > 0 && (
        <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
          <Text size="sm" c="dimmed">Всего: {data!.totalCount}</Text>
          <Group gap="sm">
            <PageSizeSelect value={pageSize} onChange={handlePageSize} />
            <Pagination value={page} onChange={setPage} total={Math.max(1, data!.totalPages)} size="sm" />
          </Group>
        </Group>
      )}

      <Modal
        opened={missingRightsItem !== null}
        onClose={() => setMissingRightsItem(null)}
        title={
          missingRightsItem
            ? `Незаполненные поля · права #${missingRightsItem.id}`
            : ''
        }
        size="sm"
        radius="md"
      >
        {missingRightsItem && (
          <Stack gap="md">
            <Text size="sm" c="dimmed">
              Продукт #{missingRightsItem.catalogProductId}
              {missingRightsItem.companySender ? ` · ${missingRightsItem.companySender}` : ''}
            </Text>
            <div>
              <Text size="sm" fw={600} mb="xs">
                Сейчас не заполнено:
              </Text>
              <List size="sm" spacing="xs" withPadding listStyleType="disc">
                {getMissingCatalogRightsIdentityFields(missingRightsItem).map((m) => (
                  <List.Item key={m}>{m}</List.Item>
                ))}
              </List>
            </div>
            <Alert color="gray" variant="light" title="Обязательные поля записи прав">
              Должны быть заданы: {CATALOG_RIGHTS_MANDATORY_FIELDS_TEXT}.
            </Alert>
            <Group justify="flex-end">
              <Button variant="default" size="xs" onClick={() => setMissingRightsItem(null)}>
                Закрыть
              </Button>
              <Button
                size="xs"
                leftSection={<IconPencil size={14} />}
                onClick={() => {
                  const row = missingRightsItem;
                  setMissingRightsItem(null);
                  handleOpenEdit(row);
                }}
              >
                Редактировать запись
              </Button>
            </Group>
          </Stack>
        )}
      </Modal>

      <Modal opened={opened} onClose={close} title={editItem ? `Редактировать права #${editItem.id}` : 'Добавить права'} size="lg">
        {editItem ? (
          <form onSubmit={submitRightsEdit}>
            <SimpleGrid cols={2} spacing="sm">
              <Select label="Отправитель" data={companyOptions} searchable
                value={editForm.values.companySenderId ? String(editForm.values.companySenderId) : null}
                onChange={(v) => editForm.setFieldValue('companySenderId', v ? Number(v) : null)} />
              <Select label="Получатель" data={companyOptions} searchable
                value={editForm.values.companyReceiverId ? String(editForm.values.companyReceiverId) : null}
                onChange={(v) => editForm.setFieldValue('companyReceiverId', v ? Number(v) : null)} />
              <Select label="Территория" data={territoryOptions} searchable
                value={editForm.values.territoryId ? String(editForm.values.territoryId) : null}
                onChange={(v) => editForm.setFieldValue('territoryId', v ? Number(v) : null)} />
              <NumberInput label="Доля %" min={1} max={100}
                value={editForm.values.share ?? ''}
                onChange={(v) => editForm.setFieldValue('share', v === '' ? null : Number(v))}
                error={editForm.errors.share} />
              <TextInput label="Начало действия" placeholder="YYYY-MM-DD" {...editForm.getInputProps('docStart')} />
              <TextInput label="Конец действия" placeholder="YYYY-MM-DD" {...editForm.getInputProps('docEnd')} />
              <TextInput label="Номер договора" {...editForm.getInputProps('docNumber')} style={{ gridColumn: 'span 2' }} />
            </SimpleGrid>
            <Group justify="flex-end" mt="md">
              <Button variant="subtle" onClick={close} disabled={isPending}>Отмена</Button>
              <Button type="submit" loading={isPending}>Сохранить</Button>
            </Group>
          </form>
        ) : (
          <form onSubmit={createForm.onSubmit((values) => createMutation.mutate(values))}>
            <SimpleGrid cols={2} spacing="sm">
              <NumberInput label="ID продукта" required
                value={createForm.values.catalogProductId || ''}
                onChange={(v) => createForm.setFieldValue('catalogProductId', Number(v))}
                error={createForm.errors.catalogProductId}
                style={{ gridColumn: 'span 2' }} />
              <Select label="Отправитель" required data={companyOptions} searchable
                value={createForm.values.companySenderId ? String(createForm.values.companySenderId) : null}
                onChange={(v) => createForm.setFieldValue('companySenderId', v ? Number(v) : 0)}
                error={createForm.errors.companySenderId} />
              <Select label="Получатель" required data={companyOptions} searchable
                value={createForm.values.companyReceiverId ? String(createForm.values.companyReceiverId) : null}
                onChange={(v) => createForm.setFieldValue('companyReceiverId', v ? Number(v) : 0)}
                error={createForm.errors.companyReceiverId} />
              <Select label="Территория" required data={territoryOptions} searchable
                value={createForm.values.territoryId ? String(createForm.values.territoryId) : null}
                onChange={(v) => createForm.setFieldValue('territoryId', v ? Number(v) : 0)}
                error={createForm.errors.territoryId} />
              <NumberInput label="Доля %" required min={1} max={100}
                value={createForm.values.share}
                onChange={(v) => createForm.setFieldValue('share', Number(v))}
                error={createForm.errors.share} />
              <TextInput label="Начало действия" placeholder="YYYY-MM-DD" required {...createForm.getInputProps('docStart')} />
              <TextInput label="Конец действия" placeholder="YYYY-MM-DD" required {...createForm.getInputProps('docEnd')} />
              <TextInput label="Номер договора" {...createForm.getInputProps('docNumber')} style={{ gridColumn: 'span 2' }} />
            </SimpleGrid>
            <Group justify="flex-end" mt="md">
              <Button variant="subtle" onClick={close} disabled={isPending}>Отмена</Button>
              <Button type="submit" loading={isPending}>Создать</Button>
            </Group>
          </form>
        )}
      </Modal>
    </>
  );
}

// ── Territories Tab ───────────────────────────────────────────────────────────

function TerritoriesTab() {
  const qc = useQueryClient();
  const [editItem, setEditItem] = useState<Territory | null>(null);
  const [opened, { open, close }] = useDisclosure(false);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useDefaultPageSize();

  const form = useForm<{ code: string; description: string }>({
    initialValues: { code: '', description: '' },
    validate: {
      code: combine(required('Укажите код территории'), maxLen(DbMax.territory.code, 'Код')),
      description: combine(
        required('Укажите наименование территории'),
        maxLen(DbMax.territory.name, 'Наименование')
      ),
    },
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['catalog-territories'],
    queryFn: () => getCatalogTerritories(),
  });

  const createMutation = useMutation({
    mutationFn: (dto: CreateTerritoryDto) => createTerritory(dto),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['catalog-territories'] }); notifications.show({ message: 'Территория создана', color: 'green' }); close(); },
    onError: (e: Error) => notifications.show({ message: e.message, color: 'red' }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, dto }: { id: number; dto: UpdateTerritoryDto }) => updateTerritory(id, dto),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['catalog-territories'] }); notifications.show({ message: 'Территория обновлена', color: 'green' }); close(); },
    onError: (e: Error) => notifications.show({ message: e.message, color: 'red' }),
  });

  const handleOpen = (item?: Territory) => {
    if (item) { setEditItem(item); form.setValues({ code: item.territoryCode, description: item.territoryName ?? '' }); }
    else { setEditItem(null); form.reset(); }
    open();
  };

  const handleSubmit = form.onSubmit((values) => {
    const dto = { territoryCode: values.code, territoryName: values.description || null };
    if (editItem) updateMutation.mutate({ id: editItem.id, dto });
    else createMutation.mutate(dto);
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const allItems = data?.items ?? [];

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return allItems;
    return allItems.filter((t) =>
      t.territoryCode.toLowerCase().includes(q) || (t.territoryName ?? '').toLowerCase().includes(q)
    );
  }, [allItems, search]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
  const pagedItems = filtered.slice((page - 1) * pageSize, page * pageSize);

  const handleSearch = (q: string) => { setSearch(q); setPage(1); };
  const handlePageSize = (v: number) => { setPageSize(v); setPage(1); };

  return (
    <>
      <Group justify="space-between" mb="sm" mt="sm" px="sm">
        <TextInput size="xs" placeholder="Поиск по коду или описанию..." style={{ width: 260 }}
          leftSection={<IconSearch size={12} />} value={search} onChange={(e) => handleSearch(e.target.value)} />
        <Button leftSection={<IconPlus size={14} />} size="xs" onClick={() => handleOpen()}>
          Добавить территорию
        </Button>
      </Group>

      {isLoading ? (
        <Center py="xl"><Loader size="sm" /></Center>
      ) : error ? (
        <Alert icon={<IconAlertCircle size={16} />} color="red" m="md">
          {error instanceof Error ? error.message : 'Ошибка загрузки'}
        </Alert>
      ) : !filtered.length ? (
        <Center py="xl"><Text c="dimmed">Территории не найдены</Text></Center>
      ) : (
        <>
          <ScrollArea>
            <Table striped highlightOnHover style={{ minWidth: 500 }}>
              <Table.Thead>
                <Table.Tr>
                  <ResizableTh>ID</ResizableTh>
                  <ResizableTh>Код</ResizableTh>
                  <ResizableTh>Описание</ResizableTh>
                  <ResizableTh />
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {pagedItems.map((t) => (
                  <Table.Tr key={t.id}>
                    <Table.Td><Text size="sm" c="dimmed">#{t.id}</Text></Table.Td>
                    <Table.Td><Badge variant="outline" size="sm">{t.territoryCode}</Badge></Table.Td>
                    <Table.Td><Text size="sm">{t.territoryName ?? '—'}</Text></Table.Td>
                    <Table.Td>
                      <ActionIcon variant="subtle" size="sm" onClick={() => handleOpen(t)}>
                        <IconPencil size={14} />
                      </ActionIcon>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
          <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {filtered.length}</Text>
            <Group gap="sm">
              <PageSizeSelect value={pageSize} onChange={handlePageSize} />
              <Pagination value={page} onChange={setPage} total={totalPages} size="sm" />
            </Group>
          </Group>
        </>
      )}

      <Modal opened={opened} onClose={close} title={editItem ? `Редактировать территорию #${editItem.id}` : 'Добавить территорию'} size="sm">
        <form onSubmit={handleSubmit}>
          <Stack gap="sm">
            <TextInput label="Код" placeholder="RU" required {...form.getInputProps('code')} />
            <TextInput label="Описание" placeholder="Россия" {...form.getInputProps('description')} />
          </Stack>
          <Group justify="flex-end" mt="md">
            <Button variant="subtle" onClick={close} disabled={isPending}>Отмена</Button>
            <Button type="submit" loading={isPending}>{editItem ? 'Сохранить' : 'Создать'}</Button>
          </Group>
        </form>
      </Modal>
    </>
  );
}

// ── Companies Tab ─────────────────────────────────────────────────────────────

function CompaniesTab() {
  const qc = useQueryClient();
  const [editItem, setEditItem] = useState<Company | null>(null);
  const [opened, { open, close }] = useDisclosure(false);
  const [phoneHintOpened, { open: openPhoneHint, close: closePhoneHint }] = useDisclosure(false);
  const [identHintOpened, { open: openIdentHint, close: closeIdentHint }] = useDisclosure(false);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useDefaultPageSize();

  const form = useForm<CreateCompanyDto>({
    initialValues: {
      legalName: '', shortName: '', companyCode: '',
      bankName: '', phoneNumber: '', country: '',
      legalAddress: '', actualAddress: '', inn: '', bic: '',
    },
    validate: {
      legalName: combine(required('Укажите юридическое наименование'), maxLen(DbMax.company.legalName, 'Юр. наименование')),
      shortName: combine(required('Укажите краткое наименование'), maxLen(DbMax.company.shortName, 'Краткое наименование')),
      companyCode: optMaxLen(DbMax.company.companyCode, 'Код компании'),
      bankName: optMaxLen(DbMax.company.bankName, 'Банк'),
      phoneNumber: phoneOptional('Телефон'),
      country: optMaxLen(DbMax.company.country, 'Страна'),
      legalAddress: optMaxLen(DbMax.company.legalAddress, 'Юр. адрес'),
      actualAddress: optMaxLen(DbMax.company.actualAddress, 'Факт. адрес'),
      inn: innOptional(),
      bic: bicOptional(),
    },
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['catalog-companies'],
    queryFn: () => getCatalogCompanies(),
  });

  const createMutation = useMutation({
    mutationFn: (dto: CreateCompanyDto) => createCompany(dto),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['catalog-companies'] }); notifications.show({ message: 'Компания создана', color: 'green' }); close(); },
    onError: (e: Error) => notifications.show({ message: e.message, color: 'red' }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, dto }: { id: number; dto: UpdateCompanyDto }) => updateCompany(id, dto),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['catalog-companies'] }); notifications.show({ message: 'Компания обновлена', color: 'green' }); close(); },
    onError: (e: Error) => notifications.show({ message: e.message, color: 'red' }),
  });

  const handleOpen = (item?: Company) => {
    if (item) {
      setEditItem(item);
      form.setValues({ legalName: item.legalName, shortName: item.shortName, companyCode: item.companyCode ?? '', bankName: '', phoneNumber: '', country: '', legalAddress: '', actualAddress: '', inn: '', bic: '' });
    } else {
      setEditItem(null);
      form.reset();
    }
    open();
  };

  const handleSubmit = form.onSubmit((values) => {
    const dto: CreateCompanyDto = {
      ...values,
      companyCode: values.companyCode || null,
      bankName: values.bankName || null,
      phoneNumber: values.phoneNumber || null,
      country: values.country || null,
      legalAddress: values.legalAddress || null,
      actualAddress: values.actualAddress || null,
      inn: values.inn || null,
      bic: values.bic || null,
    };
    if (editItem) updateMutation.mutate({ id: editItem.id, dto });
    else createMutation.mutate(dto);
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const allItems = data?.items ?? [];

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return allItems;
    return allItems.filter((c) =>
      c.legalName.toLowerCase().includes(q) ||
      c.shortName.toLowerCase().includes(q) ||
      (c.companyCode ?? '').toLowerCase().includes(q)
    );
  }, [allItems, search]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
  const pagedItems = filtered.slice((page - 1) * pageSize, page * pageSize);

  const handleSearch = (q: string) => { setSearch(q); setPage(1); };
  const handlePageSize = (v: number) => { setPageSize(v); setPage(1); };

  return (
    <>
      <Group justify="space-between" mb="sm" mt="sm" px="sm">
        <TextInput size="xs" placeholder="Поиск по наименованию или коду..." style={{ width: 280 }}
          leftSection={<IconSearch size={12} />} value={search} onChange={(e) => handleSearch(e.target.value)} />
        <Button leftSection={<IconPlus size={14} />} size="xs" onClick={() => handleOpen()}>
          Добавить компанию
        </Button>
      </Group>

      {isLoading ? (
        <Center py="xl"><Loader size="sm" /></Center>
      ) : error ? (
        <Alert icon={<IconAlertCircle size={16} />} color="red" m="md">
          {error instanceof Error ? error.message : 'Ошибка загрузки'}
        </Alert>
      ) : !filtered.length ? (
        <Center py="xl"><Text c="dimmed">Компании не найдены</Text></Center>
      ) : (
        <>
          <ScrollArea>
            <Table striped highlightOnHover style={{ minWidth: 600 }}>
              <Table.Thead>
                <Table.Tr>
                  <ResizableTh>ID</ResizableTh>
                  <ResizableTh>Юр. наименование</ResizableTh>
                  <ResizableTh>Краткое</ResizableTh>
                  <ResizableTh>Код</ResizableTh>
                  <ResizableTh />
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {pagedItems.map((c) => (
                  <Table.Tr key={c.id}>
                    <Table.Td><Text size="sm" c="dimmed">#{c.id}</Text></Table.Td>
                    <Table.Td><Text size="sm">{c.legalName}</Text></Table.Td>
                    <Table.Td><Text size="sm" fw={500}>{c.shortName}</Text></Table.Td>
                    <Table.Td><Text size="sm" c="dimmed">{c.companyCode ?? '—'}</Text></Table.Td>
                    <Table.Td>
                      <ActionIcon variant="subtle" size="sm" onClick={() => handleOpen(c)}>
                        <IconPencil size={14} />
                      </ActionIcon>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
          <Group justify="space-between" px="md" py="xs" style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}>
            <Text size="sm" c="dimmed">Всего: {filtered.length}</Text>
            <Group gap="sm">
              <PageSizeSelect value={pageSize} onChange={handlePageSize} />
              <Pagination value={page} onChange={setPage} total={totalPages} size="sm" />
            </Group>
          </Group>
        </>
      )}

      <Modal opened={opened} onClose={close} title={editItem ? `Редактировать компанию #${editItem.id}` : 'Добавить компанию'} size="lg">
        <form onSubmit={handleSubmit}>
          <SimpleGrid cols={2} spacing="sm">
            <TextInput label="Юридическое наименование" required {...form.getInputProps('legalName')} style={{ gridColumn: 'span 2' }} />
            <TextInput label="Краткое наименование" required {...form.getInputProps('shortName')} />
            <TextInput label="Код компании" {...form.getInputProps('companyCode')} />
            <TextInput label="Страна" {...form.getInputProps('country')} />
            <TextInput
              label={
                <Group gap={4} align="center">
                  <span>Телефон</span>
                  <ActionIcon size="xs" variant="transparent" color="dimmed" onClick={openPhoneHint} title="Справка по формату телефона">
                    <IconInfoCircle size={14} />
                  </ActionIcon>
                </Group>
              }
              {...form.getInputProps('phoneNumber')}
            />
            <TextInput
              label={
                <Group gap={4} align="center">
                  <span>ИНН</span>
                  <ActionIcon size="xs" variant="transparent" color="dimmed" onClick={openIdentHint} title="Справка по ИНН и БИК">
                    <IconInfoCircle size={14} />
                  </ActionIcon>
                </Group>
              }
              {...form.getInputProps('inn')}
            />
            <TextInput
              label={
                <Group gap={4} align="center">
                  <span>БИК</span>
                  <ActionIcon size="xs" variant="transparent" color="dimmed" onClick={openIdentHint} title="Справка по ИНН и БИК">
                    <IconInfoCircle size={14} />
                  </ActionIcon>
                </Group>
              }
              {...form.getInputProps('bic')}
            />
            <TextInput label="Банк" {...form.getInputProps('bankName')} style={{ gridColumn: 'span 2' }} />
            <TextInput label="Юр. адрес" {...form.getInputProps('legalAddress')} style={{ gridColumn: 'span 2' }} />
            <TextInput label="Факт. адрес" {...form.getInputProps('actualAddress')} style={{ gridColumn: 'span 2' }} />
          </SimpleGrid>
          <Group justify="flex-end" mt="md">
            <Button variant="subtle" onClick={close} disabled={isPending}>Отмена</Button>
            <Button type="submit" loading={isPending}>{editItem ? 'Сохранить' : 'Создать'}</Button>
          </Group>
        </form>
      </Modal>

      <PhoneRequirementsModal opened={phoneHintOpened} onClose={closePhoneHint} />
      <CompanyIdentifiersModal opened={identHintOpened} onClose={closeIdentHint} />
    </>
  );
}

// ── Main Page ─────────────────────────────────────────────────────────────────

export function CatalogPage() {
  return (
    <Stack gap="xl">
      <Box>
        <Title order={3} fw={600}>Каталог</Title>
        <Text c="dimmed" size="sm">Управление каталожными данными: продукты, права, территории, компании</Text>
      </Box>

      <Paper withBorder radius="md">
        <Tabs defaultValue="products">
          <Tabs.List px="sm" pt="xs">
            <Tabs.Tab value="products">Продукты</Tabs.Tab>
            <Tabs.Tab value="rights">Права</Tabs.Tab>
            <Tabs.Tab value="territories">Территории</Tabs.Tab>
            <Tabs.Tab value="companies">Компании</Tabs.Tab>
          </Tabs.List>

          <Tabs.Panel value="products"><ProductsTab /></Tabs.Panel>
          <Tabs.Panel value="rights"><RightsTab /></Tabs.Panel>
          <Tabs.Panel value="territories"><TerritoriesTab /></Tabs.Panel>
          <Tabs.Panel value="companies"><CompaniesTab /></Tabs.Panel>
        </Tabs>
      </Paper>
    </Stack>
  );
}
