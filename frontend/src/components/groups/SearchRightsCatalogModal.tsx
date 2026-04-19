import { useEffect, useState } from 'react';
import {
  Stack,
  Text,
  Paper,
  SimpleGrid,
  TextInput,
  Group,
  Button,
  Modal,
  ScrollArea,
  Table,
  Loader,
  Center,
  Alert,
  Tooltip,
  Checkbox,
  Accordion,
  Box,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import {
  IconAlertCircle,
  IconSearch,
  IconCopy,
  IconCircleCheck,
  IconAdjustments,
} from '@tabler/icons-react';
import { ResizableTh } from '../common/ResizableTh';
import { fmtDate } from '../../utils/format';
import type { SearchCatalogRightsParams } from '../../api/processing';
import type { CatalogProductRights, SuspenseGroup } from '../../types';

type MatchFieldKey = 'isrc' | 'barcode' | 'artist' | 'productName' | 'territoryCode' | 'docNumber';

const FIELD_LABELS: Record<MatchFieldKey, string> = {
  isrc: 'ISRC',
  barcode: 'Баркод',
  artist: 'Исполнитель',
  productName: 'Название продукта',
  territoryCode: 'Территория (метаправа)',
  docNumber: '№ договора (метаправа)',
};

/** Для статуса 16: продукт из каталога с fallback на метаданные группы. */
export function effectiveProductFieldsForGroup(g: SuspenseGroup) {
  const cp = g.catalogProduct;
  const md = g.groupMetaData;
  return {
    isrc: (cp?.isrc ?? md?.isrc ?? '').trim(),
    barcode: (cp?.barcode ?? md?.barcode ?? '').trim(),
    artist: (cp?.artist ?? md?.artist ?? '').trim(),
    productName: (cp?.productName ?? md?.title ?? '').trim(),
  };
}

function metaRightsFields(g: SuspenseGroup) {
  const mr = g.groupMetaRights;
  return {
    territoryCode: (mr?.territoryCode ?? '').trim(),
    docNumber: (mr?.docNumber ?? '').trim(),
  };
}

function buildMetadataSearchParams(
  selected: Record<MatchFieldKey, boolean>,
  g: SuspenseGroup
): SearchCatalogRightsParams | null {
  const p = effectiveProductFieldsForGroup(g);
  const r = metaRightsFields(g);
  const params: SearchCatalogRightsParams = { combineMode: 'and' };

  if (selected.isrc && p.isrc) params.isrc = p.isrc;
  if (selected.barcode && p.barcode) params.barcode = p.barcode;
  if (selected.artist && p.artist) params.artist = p.artist;
  if (selected.productName && p.productName) params.productName = p.productName;
  if (selected.territoryCode && r.territoryCode) params.rightsTerritoryCode = r.territoryCode;
  if (selected.docNumber && r.docNumber) params.rightsDocNumber = r.docNumber;

  const hasAny =
    (params.isrc !== undefined) ||
    (params.barcode !== undefined) ||
    (params.artist !== undefined) ||
    (params.productName !== undefined) ||
    (params.rightsTerritoryCode !== undefined) ||
    (params.rightsDocNumber !== undefined);

  if (!hasAny) return null;
  return params;
}

const EMPTY_SELECTED: Record<MatchFieldKey, boolean> = {
  isrc: false,
  barcode: false,
  artist: false,
  productName: false,
  territoryCode: false,
  docNumber: false,
};

export function SearchRightsCatalogModal({
  opened,
  onClose,
  group,
  onApplied,
  searchRights,
  copyRights,
}: {
  opened: boolean;
  onClose: () => void;
  group: SuspenseGroup;
  onApplied: () => void;
  searchRights: (params: SearchCatalogRightsParams) => Promise<CatalogProductRights[]>;
  copyRights: (rightsId: number) => Promise<void>;
}) {
  const [selected, setSelected] = useState<Record<MatchFieldKey, boolean>>({ ...EMPTY_SELECTED });
  const [manualArtist, setManualArtist] = useState('');
  const [manualIsrc, setManualIsrc] = useState('');
  const [manualProductName, setManualProductName] = useState('');
  const [manualBarcode, setManualBarcode] = useState('');
  const [results, setResults] = useState<CatalogProductRights[]>([]);
  const [searching, setSearching] = useState(false);
  const [applying, setApplying] = useState<number | null>(null);
  const [searched, setSearched] = useState(false);

  useEffect(() => {
    if (!opened) return;
    setSelected({ ...EMPTY_SELECTED });
    setManualArtist('');
    setManualIsrc('');
    setManualProductName('');
    setManualBarcode('');
    setResults([]);
    setSearched(false);
  }, [opened]);

  const pf = effectiveProductFieldsForGroup(group);
  const mr = metaRightsFields(group);

  const toggle = (key: MatchFieldKey) => {
    setSelected((s) => ({ ...s, [key]: !s[key] }));
  };

  const handleSearchByMetadata = async () => {
    const params = buildMetadataSearchParams(selected, group);
    if (!params) {
      notifications.show({
        title: 'Нет данных',
        message: 'Отметьте хотя бы одно поле с непустым значением у группы.',
        color: 'orange',
      });
      return;
    }
    setManualArtist('');
    setManualIsrc('');
    setManualProductName('');
    setManualBarcode('');
    setSearching(true);
    try {
      const data = await searchRights(params);
      setResults(data);
      setSearched(true);
    } catch (e: unknown) {
      notifications.show({
        title: 'Ошибка',
        message: e instanceof Error ? e.message : 'Ошибка поиска',
        color: 'red',
      });
    } finally {
      setSearching(false);
    }
  };

  const handleManualSearch = async () => {
    if (!manualArtist && !manualIsrc && !manualProductName && !manualBarcode) {
      notifications.show({
        title: 'Ошибка',
        message: 'Укажите хотя бы один параметр ручного поиска',
        color: 'orange',
      });
      return;
    }
    setSelected({ ...EMPTY_SELECTED });
    setSearching(true);
    try {
      const params: SearchCatalogRightsParams = {
        combineMode: 'or',
        artist: manualArtist || undefined,
        isrc: manualIsrc || undefined,
        productName: manualProductName || undefined,
        barcode: manualBarcode || undefined,
      };
      const data = await searchRights(params);
      setResults(data);
      setSearched(true);
    } catch (e: unknown) {
      notifications.show({
        title: 'Ошибка',
        message: e instanceof Error ? e.message : 'Ошибка поиска',
        color: 'red',
      });
    } finally {
      setSearching(false);
    }
  };

  const handleApply = async (rights: CatalogProductRights) => {
    setApplying(rights.id);
    try {
      await copyRights(rights.id);
      notifications.show({
        title: 'Права применены',
        message: `Договор ${rights.docNumber ?? '—'} скопирован, группа переведена в статус 88`,
        color: 'green',
        icon: <IconCircleCheck size={16} />,
      });
      onApplied();
      onClose();
    } catch (e: unknown) {
      notifications.show({
        title: 'Ошибка',
        message: e instanceof Error ? e.message : 'Ошибка применения',
        color: 'red',
      });
    } finally {
      setApplying(null);
    }
  };

  const onKey = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleManualSearch();
  };

  const renderCheckboxRow = (key: MatchFieldKey, value: string) => {
    const disabled = !value;
    return (
      <Checkbox
        key={key}
        label={
          <Group gap="xs" wrap="nowrap">
            <Text size="sm" component="span">{FIELD_LABELS[key]}</Text>
            <Text size="xs" c="dimmed" lineClamp={1} maw={280}>
              {disabled ? '— нет в данных группы' : value}
            </Text>
          </Group>
        }
        checked={selected[key]}
        disabled={disabled}
        onChange={() => toggle(key)}
      />
    );
  };

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Найти права в каталоге"
      size="xl"
      radius="md"
    >
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          Поиск записей прав у других продуктов каталога. Выбранные поля группы сопоставляются
          с каталогом; для полей продукта все отмеченные условия должны выполняться одновременно.
        </Text>

        <Paper withBorder radius="md" p="sm">
          <Text size="xs" fw={600} mb="sm" tt="uppercase" c="dimmed">
            По данным группы
          </Text>
          <Stack gap="xs">
            {renderCheckboxRow('isrc', pf.isrc)}
            {renderCheckboxRow('barcode', pf.barcode)}
            {renderCheckboxRow('artist', pf.artist)}
            {renderCheckboxRow('productName', pf.productName)}
            {renderCheckboxRow('territoryCode', mr.territoryCode)}
            {renderCheckboxRow('docNumber', mr.docNumber)}
          </Stack>
          <Group justify="flex-end" mt="sm">
            <Button
              size="xs"
              leftSection={<IconSearch size={12} />}
              loading={searching}
              onClick={handleSearchByMetadata}
            >
              Найти по выбранным полям
            </Button>
          </Group>
        </Paper>

        <Accordion variant="contained" radius="md">
          <Accordion.Item value="manual">
            <Accordion.Control icon={<IconAdjustments size={18} />}>
              Ручной поиск по каталогу
            </Accordion.Control>
            <Accordion.Panel>
              <Stack gap="sm">
                <Text size="xs" c="dimmed">
                  Любое совпадение по полям (как раньше). При нажатии «Найти» сбрасываются галочки
                  выше — дальше участвуют только введённые значения.
                </Text>
                <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
                  <TextInput
                    size="xs"
                    label="Исполнитель"
                    value={manualArtist}
                    onChange={(e) => setManualArtist(e.target.value)}
                    onKeyDown={onKey}
                  />
                  <TextInput
                    size="xs"
                    label="ISRC"
                    value={manualIsrc}
                    onChange={(e) => setManualIsrc(e.target.value)}
                    onKeyDown={onKey}
                  />
                  <TextInput
                    size="xs"
                    label="Название продукта"
                    value={manualProductName}
                    onChange={(e) => setManualProductName(e.target.value)}
                    onKeyDown={onKey}
                  />
                  <TextInput
                    size="xs"
                    label="Баркод"
                    value={manualBarcode}
                    onChange={(e) => setManualBarcode(e.target.value)}
                    onKeyDown={onKey}
                  />
                </SimpleGrid>
                <Group justify="flex-end">
                  <Button
                    size="xs"
                    leftSection={<IconSearch size={12} />}
                    loading={searching}
                    onClick={handleManualSearch}
                  >
                    Найти
                  </Button>
                </Group>
              </Stack>
            </Accordion.Panel>
          </Accordion.Item>
        </Accordion>

        {searching ? (
          <Center py="xl"><Loader color="indigo" /></Center>
        ) : searched && results.length === 0 ? (
          <Alert icon={<IconAlertCircle size={14} />} color="orange" radius="md">
            Ничего не найдено. Попробуйте изменить критерии поиска.
          </Alert>
        ) : results.length > 0 ? (
          <ScrollArea>
            <Table striped highlightOnHover style={{ minWidth: 800 }}>
              <Table.Thead>
                <Table.Tr>
                  <ResizableTh>Продукт</ResizableTh>
                  <ResizableTh>Договор</ResizableTh>
                  <ResizableTh>Отправитель</ResizableTh>
                  <ResizableTh>Получатель</ResizableTh>
                  <ResizableTh>Территория</ResizableTh>
                  <ResizableTh>Период</ResizableTh>
                  <ResizableTh>Доля</ResizableTh>
                  <ResizableTh></ResizableTh>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {results.map((r) => (
                  <Table.Tr key={r.id}>
                    <Table.Td>
                      <Stack gap={2}>
                        <Text size="sm" fw={500}>{r.catalogProduct?.productName ?? `ID: ${r.catalogProductId}`}</Text>
                        <Text size="xs" c="dimmed">{r.catalogProduct?.artist ?? ''}</Text>
                      </Stack>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{r.docNumber ?? '—'}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{r.companySender}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{r.companyReceiver}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{r.territoryCode}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="xs" c="dimmed">
                        {fmtDate(r.docStart)} – {fmtDate(r.docEnd)}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{r.share}%</Text>
                    </Table.Td>
                    <Table.Td>
                      <Tooltip label="Скопировать права в продукт группы и перевести в статус 88">
                        <Button
                          size="xs"
                          color="green"
                          variant="light"
                          leftSection={<IconCopy size={12} />}
                          loading={applying === r.id}
                          onClick={() => handleApply(r)}
                        >
                          Применить
                        </Button>
                      </Tooltip>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
        ) : (
          <Box py="xs" />
        )}
      </Stack>
    </Modal>
  );
}
