import {
  Modal,
  Stack,
  Text,
  Paper,
  ThemeIcon,
  List,
  Divider,
  Box,
  ScrollArea,
  Table,
  Group,
} from '@mantine/core';
import {
  IconInfoCircle,
  IconFileSpreadsheet,
  IconLayoutList,
  IconSearch,
  IconShieldCheck,
  IconPencil,
  IconTable,
  IconRuler2,
  IconMath,
} from '@tabler/icons-react';

const COLUMN_GROUPS: { title: string; aliases: string[] }[] = [
  { title: 'ISRC', aliases: ['ISRC'] },
  { title: 'Баркод', aliases: ['Баркод', 'Barcode'] },
  { title: 'Каталожный номер', aliases: ['Каталожный номер', 'CatalogNumber'] },
  { title: 'Формат продукта (TTkey)', aliases: ['Формат продукта', 'ProductFormatCode', 'TTkey'] },
  { title: 'Компания отправитель', aliases: ['Компания отправитель', 'SenderCompany'] },
  { title: 'Компания получатель', aliases: ['Компания получатель', 'RecipientCompany'] },
  { title: 'Оператор', aliases: ['Оператор', 'Operator'] },
  { title: 'Артист', aliases: ['Артист', 'Artist'] },
  { title: 'Название трека', aliases: ['Название', 'TrackTitle'] },
  { title: 'Тип договора', aliases: ['Тип договора', 'AgreementType'] },
  { title: 'Номер договора', aliases: ['Номер договора', 'AgreementNumber'] },
  { title: 'Код территории', aliases: ['Код территории', 'TerritoryCode'] },
  { title: 'Количество прослушиваний', aliases: ['Количество', 'Qty'] },
  { title: 'Цена за стрим (PPD)', aliases: ['Цена за стрим', 'Ppd'] },
  { title: 'Валюта', aliases: ['Валюта', 'ExchangeCurrency'] },
  { title: 'Курс обмена', aliases: ['Курс обмена', 'ExchangeRate'] },
  { title: 'Жанр', aliases: ['Жанр', 'Genre'] },
];

/** Совпадает с SuspenseLineDtoValidator и конфигурацией SuspenseLines в БД */
const TEXT_FIELD_LIMITS: { field: string; max: string; note: string }[] = [
  { field: 'ISRC', max: '15', note: 'Текст; при разборе Excel пробелы по краям обрезаются. Укладывайтесь в типичный формат ISRC (часто 12 символов).' },
  { field: 'Баркод', max: '20', note: 'Строка или цифры; не длиннее лимита в ячейке.' },
  { field: 'Каталожный номер', max: '100', note: 'Произвольная строка.' },
  { field: 'Формат (TTkey)', max: '50', note: 'Код формата продукта, например DIGI.' },
  { field: 'Компания отправитель', max: '255', note: 'Название как в отчёте.' },
  { field: 'Компания получатель', max: '255', note: 'Название как в отчёте.' },
  { field: 'Оператор', max: '255', note: 'Стриминговая площадка.' },
  { field: 'Артист', max: '255', note: 'Имя артиста.' },
  { field: 'Название трека', max: '255', note: 'Название фонограммы.' },
  { field: 'Тип договора', max: '100', note: 'Произвольная строка.' },
  { field: 'Номер договора', max: '100', note: 'При проверке прав должен совпасть с записью прав в каталоге.' },
  { field: 'Код территории', max: '10', note: 'Короткий код (например RU, US, GB).' },
  { field: 'Жанр', max: '100', note: 'Произвольная строка.' },
];

const NUMERIC_FIELD_RULES: { field: string; rule: string }[] = [
  { field: 'Количество прослушиваний (Qty)', rule: 'Целое число ≥ 0' },
  { field: 'Цена за стрим (PPD)', rule: 'Число ≥ 0, не NaN/∞; дробная часть допускается' },
  { field: 'Валюта (ExchangeCurrency)', rule: 'Десятичное: до 18 значащих цифр, из них до 6 знаков после запятой (как decimal(18,6) в БД; в отчётах часто курс с 4 знаками)' },
  { field: 'Курс обмена (ExchangeRate)', rule: 'Десятичное: до 18 значащих цифр, из них до 6 знаков после запятой (decimal(18,6))' },
];

type UploadReportRequirementsModalProps = {
  opened: boolean;
  onClose: () => void;
};

function Section({
  icon,
  color,
  title,
  children,
}: {
  icon: React.ReactNode;
  color: string;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <Paper withBorder radius="md" p="md" bg="var(--mantine-color-body)">
      <GroupHeader icon={icon} color={color} title={title} />
      <Box mt="sm">{children}</Box>
    </Paper>
  );
}

function GroupHeader({
  icon,
  color,
  title,
}: {
  icon: React.ReactNode;
  color: string;
  title: string;
}) {
  return (
    <Group gap="sm" align="flex-start" wrap="nowrap">
      <ThemeIcon size={36} radius="md" color={color} variant="light">
        {icon}
      </ThemeIcon>
      <Text fw={600} size="sm" style={{ lineHeight: 1.35 }}>
        {title}
      </Text>
    </Group>
  );
}

export function UploadReportRequirementsModal({ opened, onClose }: UploadReportRequirementsModalProps) {
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={
        <Group gap="sm" wrap="nowrap">
          <ThemeIcon size={36} radius="md" color="indigo" variant="light">
            <IconInfoCircle size={20} />
          </ThemeIcon>
          <Box>
            <Text fw={700} size="lg">
              Справка по загрузке отчёта
            </Text>
            <Text size="xs" c="dimmed" mt={2}>
              Формат файла, заголовки и как система обрабатывает строки
            </Text>
          </Box>
        </Group>
      }
      size="xl"
      scrollAreaComponent={ScrollArea.Autosize}
      styles={{ body: { paddingTop: 8 } }}
    >
      <Stack gap="lg">
        <Section icon={<IconFileSpreadsheet size={18} />} color="blue" title="Файл">
          <List spacing="xs" size="sm" c="dimmed" icon={<Text c="indigo" size="sm">•</Text>}>
            <List.Item>
              <Text span inherit c="dark">
                Допустимые расширения:{' '}
                <Text span fw={600} c="dark">
                  .xlsx
                </Text>{' '}
                и{' '}
                <Text span fw={600} c="dark">
                  .xls
                </Text>
                .
              </Text>
            </List.Item>
            <List.Item>
              <Text span inherit c="dark">
                Максимальный размер:{' '}
                <Text span fw={600} c="dark">
                  50 МБ
                </Text>
                .
              </Text>
            </List.Item>
            <List.Item>
              <Text span inherit c="dark">
                Берётся{' '}
                <Text span fw={600} c="dark">
                  первый лист
                </Text>{' '}
                книги.
              </Text>
            </List.Item>
          </List>
        </Section>

        <Section icon={<IconLayoutList size={18} />} color="grape" title="Структура Excel">
          <List spacing="xs" size="sm" c="dimmed" icon={<Text c="grape" size="sm">•</Text>}>
            <List.Item>
              <Text span inherit c="dark">
                <Text span fw={600} c="dark">
                  Первая строка
                </Text>{' '}
                — только заголовки столбцов (как в шаблоне).
              </Text>
            </List.Item>
            <List.Item>
              <Text span inherit c="dark">
                <Text span fw={600} c="dark">
                  Со второй строки
                </Text>{' '}
                — данные; каждая строка = одна запись отчёта.
              </Text>
            </List.Item>
            <List.Item>
              <Text span inherit c="dark">
                В первой строке должны присутствовать{' '}
                <Text span fw={600} c="dark">
                  все
                </Text>{' '}
                типы столбцов из таблицы ниже: для каждого логического поля нужна хотя бы одна колонка с одним из
                допустимых названий. Иначе загрузка будет отклонена с ошибкой об отсутствующих заголовках.
              </Text>
            </List.Item>
            <List.Item>
              <Text span inherit c="dark">
                Ячейки в строке данных могут быть пустыми, кроме случаев, когда нужно найти продукт или права (см. ниже).
              </Text>
            </List.Item>
            <List.Item>
              <Text span inherit c="dark">
                Полностью пустые строки в таблице данных{' '}
                <Text span fw={600} c="dark">
                  пропускаются
                </Text>{' '}
                и не попадают в загрузку.
              </Text>
            </List.Item>
          </List>
        </Section>

        <Paper withBorder radius="md" p="md" bg="var(--mantine-color-body)">
          <GroupHeader icon={<IconTable size={18} />} color="cyan" title="Названия столбцов (первая строка)" />
          <Text size="sm" c="dimmed" mt="xs" mb="md">
            Регистр букв не важен. Можно использовать русское или английское имя из списка для каждого поля.
          </Text>
          <ScrollArea.Autosize mah={280} type="auto" offsetScrollbars>
            <Table striped highlightOnHover withTableBorder withColumnBorders>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th w="32%">Поле в системе</Table.Th>
                  <Table.Th>Допустимые подписи столбца</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {COLUMN_GROUPS.map((row) => (
                  <Table.Tr key={row.title}>
                    <Table.Td>
                      <Text size="sm" fw={500}>
                        {row.title}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {row.aliases.join(' · ')}
                      </Text>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea.Autosize>
        </Paper>

        <Paper withBorder radius="md" p="md" bg="var(--mantine-color-body)">
          <GroupHeader icon={<IconRuler2 size={18} />} color="pink" title="Длина и формат полей" />
          <Text size="sm" c="dimmed" mt="xs" mb="md">
            Ограничения совпадают с полями в базе данных. Форма «Ввести вручную» проверяет их до отправки. При загрузке
            Excel соблюдайте те же лимиты: слишком длинные значения в ячейке могут привести к ошибке при сохранении.
          </Text>
          <Text fw={600} size="sm" mb="xs">
            Текстовые поля (максимум символов)
          </Text>
          <ScrollArea.Autosize mah={320} type="auto" offsetScrollbars mb="lg">
            <Table striped highlightOnHover withTableBorder withColumnBorders>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th w="32%">Поле</Table.Th>
                  <Table.Th w="10%">Макс.</Table.Th>
                  <Table.Th>Формат и примечания</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {TEXT_FIELD_LIMITS.map((row) => (
                  <Table.Tr key={row.field}>
                    <Table.Td>
                      <Text size="sm" fw={500}>
                        {row.field}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" ff="monospace">
                        {row.max}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {row.note}
                      </Text>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea.Autosize>
          <Group gap="sm" align="flex-start" wrap="nowrap" mb="xs">
            <ThemeIcon size={28} radius="md" color="pink" variant="light">
              <IconMath size={16} />
            </ThemeIcon>
            <Text fw={600} size="sm" style={{ lineHeight: 1.35 }}>
              Числовые поля
            </Text>
          </Group>
          <List spacing="xs" size="sm" withPadding>
            {NUMERIC_FIELD_RULES.map((row) => (
              <List.Item key={row.field}>
                <Text size="sm" c="dark">
                  <Text span fw={600} component="span">
                    {row.field}
                  </Text>
                  {' — '}
                  <Text span c="dimmed" component="span">
                    {row.rule}
                  </Text>
                </Text>
              </List.Item>
            ))}
          </List>
        </Paper>

        <Section icon={<IconSearch size={18} />} color="orange" title="Поиск продукта в каталоге">
          <Text size="sm" c="dimmed" mb="sm">
            Продукт ищется по одной цепочке; следующий шаг выполняется только если предыдущий не дал результата:
          </Text>
          <List spacing={6} size="sm" type="ordered" withPadding>
            <List.Item>
              <Text size="sm" c="dark">
                Совпадение по <Text fw={600}>ISRC</Text>
              </Text>
            </List.Item>
            <List.Item>
              <Text size="sm" c="dark">
                Совпадение по <Text fw={600}>баркоду</Text>
              </Text>
            </List.Item>
            <List.Item>
              <Text size="sm" c="dark">
                Одновременно <Text fw={600}>название трека</Text> и <Text fw={600}>артист</Text> (оба должны быть
                заполнены в строке)
              </Text>
            </List.Item>
            <List.Item>
              <Text size="sm" c="dark">
                Совпадение по <Text fw={600}>каталожному номеру</Text>
              </Text>
            </List.Item>
          </List>
          <Text size="sm" c="dimmed" mt="sm">
            Если продукт не найден, строка получит статус «нет продукта». Формат, жанр и другие поля сами по себе
            продукт не идентифицируют.
          </Text>
        </Section>

        <Section icon={<IconShieldCheck size={18} />} color="teal" title="Проверка прав">
          <Text size="sm" c="dimmed">
            Если продукт найден, система пытается сопоставить права по каталогу: нужны как минимум{' '}
            <Text span fw={600} c="dark">
              номер договора
            </Text>
            ,{' '}
            <Text span fw={600} c="dark">
              код территории
            </Text>{' '}
            и данные по{' '}
            <Text span fw={600} c="dark">
              компаниям отправителя и получателя
            </Text>{' '}
            (как в строке отчёта). При полном совпадении со строкой прав в каталоге строка считается успешно
            провалидированной; иначе — статус «нет прав».
          </Text>
        </Section>

        <Section icon={<IconPencil size={18} />} color="gray" title="Ручной ввод одной строки">
          <Stack gap="sm">
            <Text size="sm" c="dimmed">
              В форме «Ввести вручную» обязательно заполните{' '}
              <Text span fw={600} c="dark">
                хотя бы одно
              </Text>{' '}
              из полей: <Text span fw={600}>ISRC</Text>, <Text span fw={600}>баркод</Text> или{' '}
              <Text span fw={600}>каталожный номер</Text> — иначе строку нельзя сохранить.
            </Text>
            <Text size="sm" c="dimmed">
              Если указаны <Text span fw={600}>ID компании отправителя</Text> или{' '}
              <Text span fw={600}>получателя</Text>, значение должно быть целым числом{' '}
              <Text span fw={600}>&gt; 0</Text> (как в справочнике компаний).
            </Text>
          </Stack>
        </Section>

        <Divider label="Обработка" labelPosition="center" />
        <Text size="xs" c="dimmed" ta="center">
          После загрузки каждая строка сохраняется в базе; в интерфейсе отображаются итоги валидации и причины
          отклонения.
        </Text>
      </Stack>
    </Modal>
  );
}
