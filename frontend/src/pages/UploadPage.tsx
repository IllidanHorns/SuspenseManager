import { useState, useRef, useMemo, useCallback } from 'react';
import {
  Stack,
  Title,
  Text,
  Paper,
  Button,
  Group,
  Alert,
  SimpleGrid,
  Card,
  Table,
  ScrollArea,
  Badge,
  Box,
  Progress,
  ThemeIcon,
  Modal,
  TextInput,
  NumberInput,
  Grid,
  Select,
  ActionIcon,
  Loader,
  Center,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { useDisclosure } from '@mantine/hooks';
import {
  IconUpload,
  IconFile,
  IconAlertCircle,
  IconCircleCheck,
  IconAlertTriangle,
  IconX,
  IconPencilPlus,
  IconPackage,
  IconListDetails,
  IconInfoCircle,
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { isApiRequestError, propertyNameToFormKey } from '../api/apiError';
import { uploadFile, uploadManual } from '../api/upload';
import type { ManualSuspenseLineInput } from '../api/upload';
import { getCatalogProduct } from '../api/catalog';
import { getSuspenseById } from '../api/suspenses';
import { ResizableTh } from '../components/common/ResizableTh';
import { STATUS_LABELS, STATUS_COLORS } from '../types';
import type { ValidationResultDto, CatalogProduct, SuspenseLine, BusinessStatus, RowFormatError } from '../types';
import { fmtDateTime, fmtNumber } from '../utils/format';
import { usePermissions } from '../hooks/usePermissions';
import { DbMax, optMaxLen } from '../utils/fieldValidation';
import { UploadReportRequirementsModal } from '../components/upload/UploadReportRequirementsModal';

/** Подписи полей (имена свойств DTO в PascalCase, как в ответе API). */
const MANUAL_FIELD_LABELS: Record<string, string> = {
  Isrc: 'ISRC',
  Barcode: 'Баркод',
  CatalogNumber: 'Каталожный номер',
  ProductFormatCode: 'Формат (TTkey)',
  SenderCompany: 'Компания отправитель',
  RecipientCompany: 'Компания получатель',
  Operator: 'Оператор',
  Artist: 'Артист',
  TrackTitle: 'Название трека',
  AgreementType: 'Тип договора',
  AgreementNumber: 'Номер договора',
  TerritoryCode: 'Код территории',
  Genre: 'Жанр',
  Qty: 'Количество прослушиваний',
  Ppd: 'PPD',
  ExchangeCurrency: 'Валюта',
  ExchangeRate: 'Курс обмена',
  SenderCompanyId: 'ID компании отправителя',
  RecipientCompanyId: 'ID компании получателя',
};

const RESULT_STATUS_FILTER: { value: string; label: string }[] = [
  { value: 'all', label: 'Все статусы' },
  { value: '0', label: STATUS_LABELS[0] },
  { value: '1', label: STATUS_LABELS[1] },
  { value: '88', label: STATUS_LABELS[88] },
];

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Group gap="xs" align="flex-start" wrap="nowrap">
      <Text size="sm" c="dimmed" w={170} style={{ flexShrink: 0 }}>
        {label}
      </Text>
      <Text size="sm" style={{ wordBreak: 'break-word' }}>
        {value ?? '—'}
      </Text>
    </Group>
  );
}

export function UploadPage() {
  const { canCreateUpload } = usePermissions();
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<ValidationResultDto | null>(null);
  const [error, setError] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);

  const [manualOpened, { open: openManual, close: closeManual }] = useDisclosure(false);
  const [requirementsOpened, { open: openRequirements, close: closeRequirements }] = useDisclosure(false);
  const [manualLoading, setManualLoading] = useState(false);

  const [statusFilter, setStatusFilter] = useState<string>('all');

  const [productModalId, setProductModalId] = useState<number | null>(null);
  const [productDetail, setProductDetail] = useState<CatalogProduct | null>(null);
  const [productLoading, setProductLoading] = useState(false);

  const [suspenseModalId, setSuspenseModalId] = useState<number | null>(null);
  const [suspenseDetail, setSuspenseDetail] = useState<SuspenseLine | null>(null);
  const [suspenseLoading, setSuspenseLoading] = useState(false);

  const openProductModal = useCallback(async (id: number) => {
    setProductModalId(id);
    setProductDetail(null);
    setProductLoading(true);
    try {
      const p = await getCatalogProduct(id);
      setProductDetail(p);
    } catch (e: unknown) {
      notifications.show({
        title: 'Не удалось загрузить продукт',
        message: e instanceof Error ? e.message : 'Ошибка запроса',
        color: 'red',
      });
      setProductModalId(null);
    } finally {
      setProductLoading(false);
    }
  }, []);

  const openSuspenseModal = useCallback(async (id: number) => {
    if (id <= 0) {
      notifications.show({
        title: 'ID строки недоступен',
        message: 'Обновите бэкенд и повторите загрузку — после сохранения строки должен приходить корректный идентификатор.',
        color: 'yellow',
        icon: <IconAlertTriangle size={16} />,
      });
      return;
    }
    setSuspenseModalId(id);
    setSuspenseDetail(null);
    setSuspenseLoading(true);
    try {
      const s = await getSuspenseById(id);
      setSuspenseDetail(s);
    } catch (e: unknown) {
      notifications.show({
        title: 'Не удалось загрузить строку',
        message: e instanceof Error ? e.message : 'Ошибка запроса',
        color: 'red',
      });
      setSuspenseModalId(null);
    } finally {
      setSuspenseLoading(false);
    }
  }, []);

  const m = DbMax.suspenseLine;
  const manualForm = useForm<ManualSuspenseLineInput>({
    initialValues: {
      isrc: '',
      barcode: '',
      catalogNumber: '',
      productFormatCode: '',
      artist: '',
      trackTitle: '',
      genre: '',
      senderCompany: '',
      recipientCompany: '',
      operator: '',
      agreementType: '',
      agreementNumber: '',
      territoryCode: '',
      qty: 1,
      ppd: undefined,
      exchangeCurrency: undefined,
      exchangeRate: undefined,
    },
    validate: {
      isrc: optMaxLen(m.isrc, 'ISRC'),
      barcode: optMaxLen(m.barcode, 'Баркод'),
      catalogNumber: optMaxLen(m.catalogNumber, 'Каталожный номер'),
      productFormatCode: optMaxLen(m.productFormatCode, 'Формат (TTkey)'),
      artist: optMaxLen(m.artist, 'Артист'),
      trackTitle: optMaxLen(m.trackTitle, 'Название трека'),
      genre: optMaxLen(m.genre, 'Жанр'),
      senderCompany: optMaxLen(m.senderCompany, 'Компания отправитель'),
      recipientCompany: optMaxLen(m.recipientCompany, 'Компания получатель'),
      operator: optMaxLen(m.operator, 'Оператор'),
      agreementType: optMaxLen(m.agreementType, 'Тип договора'),
      agreementNumber: optMaxLen(m.agreementNumber, 'Номер договора'),
      territoryCode: optMaxLen(m.territoryCode, 'Код территории'),
      qty: (v) => (v != null && v < 1 ? 'Количество не менее 1' : null),
    },
  });

  const filteredResultLines = useMemo(() => {
    if (!result) return [];
    if (statusFilter === 'all') return result.lines;
    const code = Number(statusFilter) as BusinessStatus;
    return result.lines.filter((l) => l.businessStatus === code);
  }, [result, statusFilter]);

  const handleManualSubmit = async (values: ManualSuspenseLineInput) => {
    const hasIdentifier =
      (values.isrc != null && String(values.isrc).trim() !== '') ||
      (values.barcode != null && String(values.barcode).trim() !== '') ||
      (values.catalogNumber != null && String(values.catalogNumber).trim() !== '');
    if (!hasIdentifier) {
      notifications.show({
        title: 'Заполните обязательные поля',
        message: 'Укажите хотя бы одно из полей: ISRC, баркод или каталожный номер — иначе строку нельзя сохранить.',
        color: 'yellow',
        icon: <IconAlertTriangle size={16} />,
      });
      return;
    }

    manualForm.clearErrors();
    setManualLoading(true);
    try {
      const res = await uploadManual(values);
      setResult(res);
      setStatusFilter('all');
      closeManual();
      manualForm.reset();
      notifications.show({
        title: 'Строка добавлена',
        message: `Статус: ${STATUS_LABELS[res.lines[0]?.businessStatus] ?? '—'}`,
        color: 'green',
        icon: <IconCircleCheck size={16} />,
      });
    } catch (e: unknown) {
      if (isApiRequestError(e)) {
        const formErr: Record<string, string> = {};
        for (const fe of e.fieldErrors) {
          if (!fe.field) continue;
          const key = propertyNameToFormKey(fe.field);
          formErr[key] = formErr[key] ? `${formErr[key]} ${fe.message}` : fe.message;
        }
        manualForm.setErrors(formErr);

        if (e.fieldErrors.length > 0) {
          notifications.show({
            title: e.message,
            message: (
              <Stack gap={6}>
                {e.fieldErrors.map((fe) => (
                  <Text key={`${fe.field}-${fe.message}`} size="sm">
                    <Text span fw={600} inherit>
                      {MANUAL_FIELD_LABELS[fe.field] ?? fe.field}:
                    </Text>{' '}
                    {fe.message}
                  </Text>
                ))}
              </Stack>
            ),
            color: 'red',
            autoClose: 12_000,
          });
        } else {
          notifications.show({
            title: 'Ошибка',
            message: e.message,
            color: 'red',
          });
        }
      } else {
        notifications.show({
          title: 'Ошибка',
          message: e instanceof Error ? e.message : 'Ошибка при добавлении строки',
          color: 'red',
        });
      }
    } finally {
      setManualLoading(false);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    if (!f) return;
    if (!f.name.endsWith('.xlsx') && !f.name.endsWith('.xls')) {
      setError('Разрешены только файлы .xlsx и .xls');
      return;
    }
    setError('');
    setFile(f);
    setResult(null);
  };

  const handleUpload = async () => {
    if (!file) return;
    setLoading(true);
    setError('');
    try {
      const res = await uploadFile(file);
      setResult(res);
      setStatusFilter('all');
      notifications.show({
        title: 'Загрузка завершена',
        message: `Обработано ${res.totalRows} строк`,
        color: 'green',
        icon: <IconCircleCheck size={16} />,
      });
    } catch (e: unknown) {
      if (isApiRequestError(e) && e.fieldErrors.length > 0) {
        const detail = e.fieldErrors.map((fe) => fe.message).join('\n');
        setError(`${e.message}\n\n${detail}`);
      } else {
        setError(e instanceof Error ? e.message : 'Ошибка загрузки');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    const f = e.dataTransfer.files[0];
    if (f && (f.name.endsWith('.xlsx') || f.name.endsWith('.xls'))) {
      setFile(f);
      setResult(null);
      setError('');
    }
  };

  return (
    <Stack gap="xl">
      <Modal
        opened={manualOpened}
        onClose={closeManual}
        title="Ручной ввод строки суспенса"
        size="lg"
      >
        <form onSubmit={manualForm.onSubmit(handleManualSubmit)}>
          <Stack gap="sm">
            <Text size="xs" c="dimmed">Заполните хотя бы одно из полей: ISRC, Баркод или Каталожный номер</Text>
            <Grid>
              <Grid.Col span={4}>
                <TextInput label="ISRC" placeholder="RU-A0A-25-00001" {...manualForm.getInputProps('isrc')} />
              </Grid.Col>
              <Grid.Col span={4}>
                <TextInput label="Баркод" placeholder="4607012345678" {...manualForm.getInputProps('barcode')} />
              </Grid.Col>
              <Grid.Col span={4}>
                <TextInput label="Каталожный номер" placeholder="CAT-001" {...manualForm.getInputProps('catalogNumber')} />
              </Grid.Col>
              <Grid.Col span={6}>
                <TextInput label="Артист" {...manualForm.getInputProps('artist')} />
              </Grid.Col>
              <Grid.Col span={6}>
                <TextInput label="Название трека" {...manualForm.getInputProps('trackTitle')} />
              </Grid.Col>
              <Grid.Col span={4}>
                <TextInput label="Жанр" {...manualForm.getInputProps('genre')} />
              </Grid.Col>
              <Grid.Col span={4}>
                <TextInput label="Формат (TTkey)" placeholder="DIGI" {...manualForm.getInputProps('productFormatCode')} />
              </Grid.Col>
              <Grid.Col span={4}>
                <TextInput label="Код территории" placeholder="RU" {...manualForm.getInputProps('territoryCode')} />
              </Grid.Col>
              <Grid.Col span={6}>
                <TextInput label="Компания отправитель" {...manualForm.getInputProps('senderCompany')} />
              </Grid.Col>
              <Grid.Col span={6}>
                <TextInput label="Компания получатель" {...manualForm.getInputProps('recipientCompany')} />
              </Grid.Col>
              <Grid.Col span={4}>
                <TextInput label="Оператор" placeholder="Yandex Music" {...manualForm.getInputProps('operator')} />
              </Grid.Col>
              <Grid.Col span={4}>
                <TextInput label="Тип договора" {...manualForm.getInputProps('agreementType')} />
              </Grid.Col>
              <Grid.Col span={4}>
                <TextInput label="Номер договора" {...manualForm.getInputProps('agreementNumber')} />
              </Grid.Col>
              <Grid.Col span={4}>
                <NumberInput label="Кол-во прослушиваний" min={0} {...manualForm.getInputProps('qty')} />
              </Grid.Col>
              <Grid.Col span={4}>
                <NumberInput label="Цена за стрим (PPD)" min={0} decimalScale={6} {...manualForm.getInputProps('ppd')} />
              </Grid.Col>
              <Grid.Col span={4}>
                <TextInput label="Валюта" placeholder="RUB" {...manualForm.getInputProps('exchangeCurrency')} />
              </Grid.Col>
            </Grid>
            <Group justify="flex-end" mt="sm">
              <Button variant="subtle" color="gray" onClick={closeManual}>Отмена</Button>
              <Button type="submit" loading={manualLoading} leftSection={<IconPencilPlus size={16} />}>
                Добавить строку
              </Button>
            </Group>
          </Stack>
        </form>
      </Modal>

      <Modal
        opened={productModalId !== null}
        onClose={() => {
          setProductModalId(null);
          setProductDetail(null);
        }}
        title="Продукт каталога"
        size="lg"
      >
        {productLoading ? (
          <Center py="xl">
            <Loader />
          </Center>
        ) : productDetail ? (
          <Stack gap="xs">
            <DetailRow label="ID" value={productDetail.id} />
            <DetailRow label="ISRC" value={productDetail.isrc} />
            <DetailRow label="Баркод" value={productDetail.barcode} />
            <DetailRow label="Каталожный номер" value={productDetail.catalogNumber} />
            <DetailRow label="Название" value={productDetail.productName} />
            <DetailRow label="Артист" value={productDetail.artist} />
            <DetailRow label="Жанр" value={productDetail.genre} />
            <DetailRow label="Длительность" value={productDetail.duration} />
            <DetailRow label="Дата релиза" value={productDetail.releaseDate ? fmtDateTime(productDetail.releaseDate) : null} />
            <DetailRow label="Тип продукта (ID)" value={productDetail.productTypeId ?? '—'} />
            <DetailRow label="Создан" value={fmtDateTime(productDetail.createTime)} />
            <DetailRow label="Архив" value={productDetail.archiveLevel} />
          </Stack>
        ) : null}
      </Modal>

      <Modal
        opened={suspenseModalId !== null}
        onClose={() => {
          setSuspenseModalId(null);
          setSuspenseDetail(null);
        }}
        title="Данные строки суспенса"
        size="lg"
      >
        {suspenseLoading ? (
          <Center py="xl">
            <Loader />
          </Center>
        ) : suspenseDetail ? (
          <Stack gap="xs">
            <DetailRow label="ID" value={suspenseDetail.id} />
            <DetailRow label="Статус" value={STATUS_LABELS[suspenseDetail.businessStatus] ?? suspenseDetail.businessStatus} />
            <DetailRow label="Причина суспенса" value={suspenseDetail.causeSuspense} />
            <DetailRow label="ISRC" value={suspenseDetail.isrc} />
            <DetailRow label="Баркод" value={suspenseDetail.barcode} />
            <DetailRow label="Каталожный номер" value={suspenseDetail.catalogNumber} />
            <DetailRow label="Формат (TTkey)" value={suspenseDetail.productFormatCode} />
            <DetailRow label="Артист" value={suspenseDetail.artist} />
            <DetailRow label="Название трека" value={suspenseDetail.trackTitle} />
            <DetailRow label="Жанр" value={suspenseDetail.genre} />
            <DetailRow label="Компания отправитель" value={suspenseDetail.senderCompany} />
            <DetailRow label="Компания получатель" value={suspenseDetail.recipientCompany} />
            <DetailRow label="Оператор" value={suspenseDetail.operator} />
            <DetailRow label="Тип договора" value={suspenseDetail.agreementType} />
            <DetailRow label="Номер договора" value={suspenseDetail.agreementNumber} />
            <DetailRow label="Территория" value={suspenseDetail.territoryCode} />
            <DetailRow label="Кол-во прослушиваний" value={fmtNumber(suspenseDetail.qty)} />
            <DetailRow label="PPD" value={suspenseDetail.ppd != null ? String(suspenseDetail.ppd) : '—'} />
            <DetailRow
              label="Валюта / курс"
              value={
                suspenseDetail.exchangeCurrency != null || suspenseDetail.exchangeRate != null
                  ? `${String(suspenseDetail.exchangeCurrency ?? '—')} / ${fmtNumber(suspenseDetail.exchangeRate)}`
                  : '—'
              }
            />
            <DetailRow label="ID продукта в каталоге" value={suspenseDetail.productId ?? '—'} />
            <DetailRow label="ID группы" value={suspenseDetail.groupId ?? '—'} />
            <DetailRow label="Создана" value={fmtDateTime(suspenseDetail.createTime)} />
            <DetailRow label="Изменена" value={fmtDateTime(suspenseDetail.changeTime)} />
            <DetailRow label="Архив" value={suspenseDetail.archiveLevel} />
          </Stack>
        ) : null}
      </Modal>

      <UploadReportRequirementsModal opened={requirementsOpened} onClose={closeRequirements} />

      <Group justify="space-between" align="flex-start" wrap="wrap" gap="md">
        <Box style={{ flex: '1 1 220px' }}>
          <Title order={3} fw={600}>Загрузка отчёта</Title>
          <Text c="dimmed" size="sm">Загрузите Excel-файл со стриминговым отчётом для валидации</Text>
        </Box>
        <Group gap="xs" wrap="wrap" justify="flex-end">
          <Button
            variant="light"
            color="gray"
            leftSection={<IconInfoCircle size={18} />}
            onClick={openRequirements}
          >
            Справка по файлу
          </Button>
          {canCreateUpload && (
            <Button
              variant="light"
              leftSection={<IconPencilPlus size={16} />}
              onClick={openManual}
            >
              Ввести вручную
            </Button>
          )}
        </Group>
      </Group>

      {canCreateUpload && (
        <>
          <Paper
            withBorder
            radius="md"
            p="xl"
            style={{
              borderStyle: 'dashed',
              borderWidth: 2,
              textAlign: 'center',
              cursor: 'pointer',
            }}
            onDragOver={(e) => e.preventDefault()}
            onDrop={handleDrop}
            onClick={() => inputRef.current?.click()}
          >
            <input
              ref={inputRef}
              type="file"
              accept=".xlsx,.xls"
              style={{ display: 'none' }}
              onChange={handleFileChange}
            />
            <ThemeIcon size={56} radius="xl" color="indigo" variant="light" mx="auto" mb="md">
              <IconUpload size={28} />
            </ThemeIcon>
            <Text size="lg" fw={500} mb={4}>
              {file ? file.name : 'Перетащите файл или нажмите для выбора'}
            </Text>
            <Text size="sm" c="dimmed">Поддерживаются форматы .xlsx и .xls (до 50 МБ)</Text>

            {file && (
              <Group justify="center" mt="md" gap="xs">
                <IconFile size={16} />
                <Text size="sm" fw={500}>{file.name}</Text>
                <Text size="sm" c="dimmed">({(file.size / 1024).toFixed(0)} KB)</Text>
              </Group>
            )}
          </Paper>

          {error && (
            <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md" title="Не удалось обработать файл">
              <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>{error}</Text>
            </Alert>
          )}

          {file && (
            <Group>
              <Button
                leftSection={<IconUpload size={16} />}
                color="indigo"
                loading={loading}
                onClick={handleUpload}
              >
                Загрузить и валидировать
              </Button>
              <Button
                variant="subtle"
                color="gray"
                leftSection={<IconX size={16} />}
                onClick={() => { setFile(null); setResult(null); setError(''); }}
              >
                Очистить
              </Button>
            </Group>
          )}
        </>
      )}

      {result && (
        <Stack gap="md">
          <SimpleGrid cols={{ base: 2, sm: 4 }} spacing="md">
            {[
              { label: 'Всего строк', value: result.totalRows, color: 'blue', icon: IconFile },
              { label: 'Прошло валидацию', value: result.validatedCount, color: 'green', icon: IconCircleCheck },
              { label: 'Нет продукта', value: result.noProductCount, color: 'orange', icon: IconAlertTriangle },
              { label: 'Нет прав', value: result.noRightsCount, color: 'red', icon: IconAlertCircle },
            ].map(({ label, value, color, icon: Icon }) => (
              <Card withBorder radius="md" p="md" key={label}>
                <Group justify="space-between" mb={4}>
                  <Text size="xs" c="dimmed" fw={600} tt="uppercase">{label}</Text>
                  <ThemeIcon size={28} radius="md" color={color} variant="light">
                    <Icon size={14} />
                  </ThemeIcon>
                </Group>
                <Text size="xl" fw={700}>{value}</Text>
                <Progress
                  value={result.totalRows > 0 ? (value / result.totalRows) * 100 : 0}
                  color={color}
                  size="xs"
                  mt="sm"
                  radius="xl"
                />
              </Card>
            ))}
          </SimpleGrid>

          {result.rowFormatErrors && result.rowFormatErrors.length > 0 && (
            <Alert
              icon={<IconAlertTriangle size={16} />}
              color="yellow"
              radius="md"
              title={`Пропущено строк из-за ошибок формата: ${result.rowFormatErrors.length}`}
            >
              <Text size="sm" c="dimmed" mb={6}>
                Эти строки не сохранены в базе. Исправьте данные и загрузите файл повторно.
              </Text>
              <ScrollArea mah={160}>
                <Stack gap={2}>
                  {result.rowFormatErrors.map((fe: RowFormatError) => (
                    <Text key={fe.rowNumber} size="sm">
                      <Text span fw={600} inherit>Строка {fe.rowNumber}</Text>
                      {fe.isrc ? <Text span c="dimmed" inherit> ({fe.isrc})</Text> : null}
                      {': '}
                      {fe.errors.join('; ')}
                    </Text>
                  ))}
                </Stack>
              </ScrollArea>
            </Alert>
          )}

          <Paper withBorder radius="md">
            <Group
              justify="space-between"
              align="center"
              wrap="wrap"
              gap="sm"
              p="md"
              style={{ borderBottom: '1px solid var(--mantine-color-default-border)' }}
            >
              <Text fw={600}>
                Результаты валидации ({filteredResultLines.length}
                {filteredResultLines.length !== result.lines.length ? ` из ${result.lines.length}` : ''} строк)
              </Text>
              <Select
                w={280}
                placeholder="Фильтр по статусу"
                data={RESULT_STATUS_FILTER}
                value={statusFilter}
                onChange={(v) => setStatusFilter(v ?? 'all')}
                clearable={false}
              />
            </Group>
            <ScrollArea h={400}>
              <Table striped highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <ResizableTh>ID строки</ResizableTh>
                    <ResizableTh>Статус</ResizableTh>
                    <ResizableTh>Причина</ResizableTh>
                    <ResizableTh>ID продукта</ResizableTh>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {filteredResultLines.map((line, idx) => (
                    <Table.Tr key={`${line.suspenseLineId}-${idx}`}>
                      <Table.Td>
                        <Group gap={6} wrap="nowrap">
                          <Text size="sm" component="span">
                            {line.suspenseLineId}
                          </Text>
                          <ActionIcon
                            variant="subtle"
                            color="indigo"
                            size="sm"
                            aria-label="Данные строки"
                            onClick={() => void openSuspenseModal(line.suspenseLineId)}
                          >
                            <IconListDetails size={16} />
                          </ActionIcon>
                        </Group>
                      </Table.Td>
                      <Table.Td>
                        <Badge color={STATUS_COLORS[line.businessStatus] ?? 'gray'} variant="light" size="sm">
                          {STATUS_LABELS[line.businessStatus] ?? line.businessStatus}
                        </Badge>
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm" c="dimmed">{line.causeSuspense ?? '—'}</Text>
                      </Table.Td>
                      <Table.Td>
                        <Group gap={6} wrap="nowrap">
                          <Text size="sm" component="span">
                            {line.productId ?? '—'}
                          </Text>
                          {line.productId != null && (
                            <ActionIcon
                              variant="subtle"
                              color="teal"
                              size="sm"
                              aria-label="Карточка продукта"
                              onClick={() => void openProductModal(line.productId!)}
                            >
                              <IconPackage size={16} />
                            </ActionIcon>
                          )}
                        </Group>
                      </Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </ScrollArea>
          </Paper>
        </Stack>
      )}
    </Stack>
  );
}
