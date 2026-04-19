import { useEffect, useMemo, useRef, useState, type RefObject } from 'react';
import {
  Box,
  Stack,
  Title,
  Text,
  Paper,
  Badge,
  ThemeIcon,
  SimpleGrid,
  List,
  Divider,
  Anchor,
  Group,
  Tabs,
  Button,
  TextInput,
  CloseButton,
} from '@mantine/core';
import {
  IconSearch,
  IconInfoCircle,
  IconMusicOff,
  IconShieldOff,
  IconUsersGroup,
  IconClockPause,
  IconCircleCheck,
  IconBuildingSkyscraper,
  IconArrowRight,
  IconLayoutDashboard,
  IconListDetails,
  IconVocabulary,
  IconFileTypePdf,
  IconGitBranch,
} from '@tabler/icons-react';
import { Link } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import {
  DiagramGroupingFlow,
  DiagramNoProductBranchFlow,
  DiagramNoRightsBranchFlow,
  DiagramUploadValidateFlow,
} from '../components/knowledge/ProcessFlowDiagrams';
import { KNOWLEDGE_PROCESS_BLOCKS, matchesProcessBlock } from '../data/knowledgeProcessesData';
import { GLOSSARY_ENTRIES, STATUS_TEXT_ENTRIES, type GlossaryEntry } from '../data/knowledgeBaseData';
import { STATUS_COLORS, STATUS_LABELS } from '../types';

type KnowledgePdfModule = typeof import('../utils/knowledgePdfExport');

type StatusEntry = (typeof STATUS_TEXT_ENTRIES)[number] & { icon: typeof IconMusicOff };

const STATUS_ICON_BY_CODE: Record<number, typeof IconMusicOff> = {
  0: IconMusicOff,
  1: IconShieldOff,
  15: IconUsersGroup,
  16: IconUsersGroup,
  30: IconClockPause,
  32: IconClockPause,
  88: IconCircleCheck,
  120: IconBuildingSkyscraper,
  320: IconBuildingSkyscraper,
};

const STATUS_ENTRIES: StatusEntry[] = STATUS_TEXT_ENTRIES.map((e) => ({
  ...e,
  icon: STATUS_ICON_BY_CODE[e.code] ?? IconMusicOff,
}));

function norm(s: string): string {
  return s.trim().toLowerCase();
}

function matchesStatus(entry: StatusEntry, needle: string): boolean {
  if (!needle) return true;
  const label = STATUS_LABELS[entry.code] ?? '';
  const blob = [
    String(entry.code),
    label,
    entry.shortTitle,
    entry.summary,
    entry.detail,
    entry.example,
  ]
    .join(' ')
    .toLowerCase();
  return blob.includes(needle);
}

function matchesGlossary(entry: GlossaryEntry, needle: string): boolean {
  if (!needle) return true;
  const blob = `${entry.term} ${entry.definition}`.toLowerCase();
  return blob.includes(needle);
}

function StatusCard({ entry }: { entry: StatusEntry }) {
  const label = STATUS_LABELS[entry.code] ?? `Статус ${entry.code}`;
  const color = STATUS_COLORS[entry.code] ?? 'gray';
  const Icon = entry.icon;

  return (
    <Paper
      shadow="sm"
      radius="lg"
      p="lg"
      withBorder
      style={{
        borderLeftWidth: 4,
        borderLeftColor: `var(--mantine-color-${color}-filled)`,
        overflow: 'hidden',
      }}
    >
      <Stack gap="md">
        <GroupHeader code={entry.code} label={label} color={color} Icon={Icon} shortTitle={entry.shortTitle} />
        <Text size="sm" c="dimmed" lh={1.65}>
          {entry.summary}
        </Text>
        <Text size="sm" lh={1.7}>
          {entry.detail}
        </Text>
        <Paper bg="var(--mantine-color-body)" p="md" radius="md" style={{ border: '1px solid var(--mantine-color-default-border)' }}>
          <Text size="xs" tt="uppercase" fw={700} c="dimmed" mb={6}>
            Пример
          </Text>
          <Text size="sm" lh={1.65} style={{ fontStyle: 'italic' }}>
            {entry.example}
          </Text>
        </Paper>
      </Stack>
    </Paper>
  );
}

function GroupHeader({
  code,
  label,
  color,
  Icon,
  shortTitle,
}: {
  code: number;
  label: string;
  color: string;
  Icon: typeof IconMusicOff;
  shortTitle: string;
}) {
  return (
    <Stack gap="sm">
      <Group gap="sm" wrap="wrap" align="flex-start">
        <ThemeIcon size={44} radius="md" variant="light" color={color}>
          <Icon size={24} stroke={1.5} />
        </ThemeIcon>
        <Box style={{ flex: 1, minWidth: 200 }}>
          <Group gap="xs" mb={6} wrap="wrap">
            <Badge size="lg" variant="filled" color={color} radius="sm" tt="none" fw={700}>
              {code}
            </Badge>
            <Text fw={700} size="lg" lh={1.3}>
              {label}
            </Text>
          </Group>
          <Text size="sm" c="indigo" fw={600}>
            {shortTitle}
          </Text>
        </Box>
      </Group>
    </Stack>
  );
}

/** Раздел «Описание статусов» — только карточки статусов и вводный блок по ним. */
function KnowledgeStatusesSection({
  searchQuery,
  knowledgePdfRef,
  pdfReady,
}: {
  searchQuery: string;
  knowledgePdfRef: RefObject<KnowledgePdfModule | null>;
  pdfReady: boolean;
}) {
  const needle = norm(searchQuery);
  const entries = useMemo(
    () => STATUS_ENTRIES.filter((e) => matchesStatus(e, needle)),
    [needle]
  );

  return (
    <Stack gap="lg">
      <Group justify="flex-end">
        <Button
          variant="light"
          color="indigo"
          size="sm"
          leftSection={<IconFileTypePdf size={16} stroke={1.5} />}
          disabled={!pdfReady}
          title={!pdfReady ? 'Подготовка модуля PDF…' : undefined}
          onClick={() => {
            const m = knowledgePdfRef.current;
            if (!m) return;
            void m
              .downloadKnowledgeStatusesPdf()
              .catch((err: unknown) => {
                console.error(err);
                notifications.show({
                  color: 'red',
                  title: 'Ошибка PDF',
                  message: err instanceof Error ? err.message : 'Не удалось скачать файл',
                });
              });
          }}
        >
          Скачать PDF
        </Button>
      </Group>

      {!needle && (
        <Paper shadow="xs" radius="lg" p={{ base: 'md', sm: 'xl' }} withBorder>
          <Stack gap="md">
            <Group gap="sm" wrap="nowrap" align="flex-start">
              <ThemeIcon color="blue" variant="light" radius="md" size="lg" visibleFrom="xs">
                <IconInfoCircle size={20} />
              </ThemeIcon>
              <div style={{ flex: 1 }}>
                <Title order={3} size="h4" fw={700}>
                  Описание статусов
                </Title>
                <Text size="sm" c="dimmed" mt={6} lh={1.65}>
                  Код статуса — то же число, что в таблицах и фильтрах. Ниже — смысл в бизнес-логике и типичные ситуации.
                </Text>
              </div>
            </Group>

            <Divider />

            <List spacing="xs" size="sm" c="dimmed" icon={<IconArrowRight size={14} style={{ marginTop: 4 }} />}>
              <List.Item>
                Статусы <strong>0 и 1</strong> — у отдельных строк суспенсов до группировки.
              </List.Item>
              <List.Item>
                Статусы <strong>15 и 16</strong> — активные группы; <strong>30 и 32</strong> — отложенные группы.
              </List.Item>
              <List.Item>
                <strong>88</strong> — успешное завершение; <strong>120 и 320</strong> — эскалация в бэк-офис по двум веткам.
              </List.Item>
            </List>

            <Text size="sm" c="dimmed">
              Общие схемы потоков — во вкладке «Бизнес-процессы» этой базы знаний. Рабочие экраны:{' '}
              <Anchor component={Link} to="/upload" fw={600}>
                Загрузка
              </Anchor>
              ,{' '}
              <Anchor component={Link} to="/grouping" fw={600}>
                Группировка
              </Anchor>
              ,{' '}
              <Anchor component={Link} to="/groups" fw={600}>
                Сохранённые группы
              </Anchor>
              .
            </Text>
          </Stack>
        </Paper>
      )}

      {needle && entries.length === 0 && (
        <Text size="sm" c="dimmed" ta="center" py="xl">
          По запросу совпадений среди статусов нет. Попробуйте другой текст или вкладки «Словарь», «Бизнес-процессы».
        </Text>
      )}

      <SimpleGrid cols={{ base: 1, lg: 1 }} spacing="lg">
        {entries.map((entry) => (
          <StatusCard key={entry.code} entry={entry} />
        ))}
      </SimpleGrid>

      {!needle && (
        <Paper p="lg" radius="md" withBorder variant="light" color="gray">
          <Text size="sm" c="dimmed" ta="center" lh={1.65}>
            Тексты носят учебный характер и отражают типовой сценарий. Фактические правила учёта и договоров уточняйте у вашего внутреннего регламента.
          </Text>
        </Paper>
      )}
    </Stack>
  );
}

function GlossaryTermCard({ entry }: { entry: GlossaryEntry }) {
  return (
    <Paper
      withBorder
      p="md"
      radius="md"
      shadow="xs"
      style={{
        borderLeftWidth: 4,
        borderLeftColor: 'var(--mantine-color-teal-filled)',
      }}
    >
      <Text fw={700} size="sm" mb={8} c="teal">
        {entry.term}
      </Text>
      <Text size="sm" c="dimmed" lh={1.7}>
        {entry.definition}
      </Text>
    </Paper>
  );
}

/** Раздел «Словарь» — краткие определения терминов. */
function KnowledgeGlossarySection({
  searchQuery,
  knowledgePdfRef,
  pdfReady,
}: {
  searchQuery: string;
  knowledgePdfRef: RefObject<KnowledgePdfModule | null>;
  pdfReady: boolean;
}) {
  const needle = norm(searchQuery);
  const entries = useMemo(
    () => GLOSSARY_ENTRIES.filter((e) => matchesGlossary(e, needle)),
    [needle]
  );

  return (
    <Stack gap="lg">
      <Group justify="flex-end">
        <Button
          variant="light"
          color="teal"
          size="sm"
          leftSection={<IconFileTypePdf size={16} stroke={1.5} />}
          disabled={!pdfReady}
          title={!pdfReady ? 'Подготовка модуля PDF…' : undefined}
          onClick={() => {
            const m = knowledgePdfRef.current;
            if (!m) return;
            void m
              .downloadKnowledgeGlossaryPdf()
              .catch((err: unknown) => {
                console.error(err);
                notifications.show({
                  color: 'red',
                  title: 'Ошибка PDF',
                  message: err instanceof Error ? err.message : 'Не удалось скачать файл',
                });
              });
          }}
        >
          Скачать PDF
        </Button>
      </Group>

      {!needle && (
        <Paper shadow="xs" radius="lg" p={{ base: 'md', sm: 'xl' }} withBorder>
          <Group gap="sm" wrap="nowrap" align="flex-start">
            <ThemeIcon color="teal" variant="light" radius="md" size="lg" visibleFrom="xs">
              <IconVocabulary size={22} />
            </ThemeIcon>
            <div style={{ flex: 1 }}>
              <Title order={3} size="h4" fw={700}>
                Словарь определений
              </Title>
              <Text size="sm" c="dimmed" mt={6} lh={1.65}>
                Термины в алфавитном порядке. Формулировки облегчают чтение статусов, отчётов и переписку с коллегами.
              </Text>
            </div>
          </Group>
        </Paper>
      )}

      {needle && entries.length === 0 && (
        <Text size="sm" c="dimmed" ta="center" py="xl">
          По запросу совпадений в словаре нет. Попробуйте вкладки «Описание статусов» или «Бизнес-процессы».
        </Text>
      )}

      <Stack gap="sm">
        {entries.map((entry) => (
          <GlossaryTermCard key={entry.term} entry={entry} />
        ))}
      </Stack>
    </Stack>
  );
}

function ProcessDiagramForBlock({ blockId }: { blockId: string }) {
  switch (blockId) {
    case 'upload-validate':
      return <DiagramUploadValidateFlow />;
    case 'grouping':
      return <DiagramGroupingFlow />;
    case 'branch-no-product':
      return <DiagramNoProductBranchFlow />;
    case 'branch-no-rights':
      return <DiagramNoRightsBranchFlow />;
    default:
      return null;
  }
}

/** Раздел «Бизнес-процессы» — общий ход работы и схемы. */
function KnowledgeProcessesSection({ searchQuery }: { searchQuery: string }) {
  const needle = norm(searchQuery);
  const blocks = useMemo(
    () => KNOWLEDGE_PROCESS_BLOCKS.filter((b) => matchesProcessBlock(b, needle)),
    [needle]
  );

  return (
    <Stack gap="lg">
      {!needle && (
        <Paper shadow="xs" radius="lg" p={{ base: 'md', sm: 'xl' }} withBorder>
          <Group gap="sm" wrap="nowrap" align="flex-start">
            <ThemeIcon color="violet" variant="light" radius="md" size="lg" visibleFrom="xs">
              <IconGitBranch size={22} stroke={1.5} />
            </ThemeIcon>
            <div style={{ flex: 1 }}>
              <Title order={3} size="h4" fw={700}>
                Бизнес-процессы
              </Title>
              <Text size="sm" c="dimmed" mt={6} lh={1.65}>
                Упрощённые схемы: от загрузки отчёта до группировки и двух веток обработки. Детали статусов — во вкладке «Описание статусов»; термины — в «Словаре».
              </Text>
            </div>
          </Group>
        </Paper>
      )}

      {needle && blocks.length === 0 && (
        <Text size="sm" c="dimmed" ta="center" py="xl">
          По запросу совпадений в этом разделе нет. Попробуйте вкладки «Описание статусов» или «Словарь».
        </Text>
      )}

      <Stack gap="xl">
        {blocks.map((block) => (
          <Paper key={block.id} shadow="sm" radius="lg" p={{ base: 'md', sm: 'lg' }} withBorder>
            <Stack gap="md">
              <Title order={4} size="h5" fw={700}>
                {block.title}
              </Title>
              {block.paragraphs.map((p, i) => (
                <Text key={i} size="sm" lh={1.7} c="dimmed">
                  {p}
                </Text>
              ))}
              {['upload-validate', 'grouping', 'branch-no-product', 'branch-no-rights'].includes(block.id) ? (
                <Box
                  p={{ base: 'sm', sm: 'md' }}
                  style={{
                    borderRadius: 12,
                    background: 'var(--mantine-color-body)',
                    border: '1px solid var(--mantine-color-default-border)',
                  }}
                >
                  <ProcessDiagramForBlock blockId={block.id} />
                </Box>
              ) : null}
            </Stack>
          </Paper>
        ))}
      </Stack>
    </Stack>
  );
}

/** Обзор базы знаний: зачем раздел и навигация по подразделам. */
function KnowledgeOverviewSection({
  onOpenStatuses,
  onOpenGlossary,
  onOpenProcesses,
}: {
  onOpenStatuses: () => void;
  onOpenGlossary: () => void;
  onOpenProcesses: () => void;
}) {
  return (
    <Stack gap="lg">
      <Paper radius="lg" p="xl" withBorder shadow="xs">
        <Stack gap="md">
          <Title order={3} size="h4" fw={700}>
            Добро пожаловать
          </Title>
          <Text size="sm" lh={1.7} c="dimmed">
            Здесь собраны справочные материалы по SuspenseManager: расшифровка статусов, словарь терминов, общий вид бизнес-процессов и ссылки на рабочие экраны. Разделы будут пополняться — используйте вкладки выше или карточки ниже.
          </Text>
        </Stack>
      </Paper>

      <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
        <Paper
          radius="lg"
          p="lg"
          withBorder
          style={{
            cursor: 'pointer',
            transition: 'transform 0.15s ease, box-shadow 0.15s ease',
          }}
          onClick={onOpenStatuses}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              onOpenStatuses();
            }
          }}
        >
          <Group justify="space-between" wrap="nowrap" align="flex-start" gap="md">
            <ThemeIcon size={48} radius="md" variant="gradient" gradient={{ from: 'indigo', to: 'violet', deg: 135 }}>
              <IconListDetails size={26} stroke={1.5} />
            </ThemeIcon>
            <Stack gap={6} style={{ flex: 1 }}>
              <Text fw={700} size="lg">
                Описание статусов
              </Text>
              <Text size="sm" c="dimmed" lh={1.65}>
                Все коды от 0 до 320: что означает каждый статус, как устроены ветки «нет продукта» и «нет прав», примеры из практики.
              </Text>
              <Button variant="light" color="indigo" size="xs" w="fit-content" mt={4} onClick={(e) => { e.stopPropagation(); onOpenStatuses(); }}>
                Открыть раздел
              </Button>
            </Stack>
          </Group>
        </Paper>

        <Paper
          radius="lg"
          p="lg"
          withBorder
          style={{
            cursor: 'pointer',
            transition: 'transform 0.15s ease, box-shadow 0.15s ease',
          }}
          onClick={onOpenGlossary}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              onOpenGlossary();
            }
          }}
        >
          <Group justify="space-between" wrap="nowrap" align="flex-start" gap="md">
            <ThemeIcon size={48} radius="md" variant="gradient" gradient={{ from: 'teal', to: 'cyan', deg: 135 }}>
              <IconVocabulary size={26} stroke={1.5} />
            </ThemeIcon>
            <Stack gap={6} style={{ flex: 1 }}>
              <Text fw={700} size="lg">
                Словарь определений
              </Text>
              <Text size="sm" c="dimmed" lh={1.65}>
                ISRC, PPD, метаданные, ветки обработки, бэк-офис и другие понятия — в одном месте, коротко и по делу.
              </Text>
              <Button variant="light" color="teal" size="xs" w="fit-content" mt={4} onClick={(e) => { e.stopPropagation(); onOpenGlossary(); }}>
                Открыть раздел
              </Button>
            </Stack>
          </Group>
        </Paper>

        <Paper
          radius="lg"
          p="lg"
          withBorder
          style={{
            cursor: 'pointer',
            transition: 'transform 0.15s ease, box-shadow 0.15s ease',
          }}
          onClick={onOpenProcesses}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              onOpenProcesses();
            }
          }}
        >
          <Group justify="space-between" wrap="nowrap" align="flex-start" gap="md">
            <ThemeIcon size={48} radius="md" variant="gradient" gradient={{ from: 'violet', to: 'grape', deg: 135 }}>
              <IconGitBranch size={26} stroke={1.5} />
            </ThemeIcon>
            <Stack gap={6} style={{ flex: 1 }}>
              <Text fw={700} size="lg">
                Бизнес-процессы
              </Text>
              <Text size="sm" c="dimmed" lh={1.65}>
                От отчёта до группировки и двух веток: схемы потоков в общих чертах — как связаны загрузка, статусы и работа оператора.
              </Text>
              <Button variant="light" color="violet" size="xs" w="fit-content" mt={4} onClick={(e) => { e.stopPropagation(); onOpenProcesses(); }}>
                Открыть раздел
              </Button>
            </Stack>
          </Group>
        </Paper>
      </SimpleGrid>

      <Text size="xs" c="dimmed" ta="center">
        Сценарии по ролям и разбор ошибок можно добавить сюда позже.
      </Text>
    </Stack>
  );
}

export function KnowledgeBasePage() {
  const [section, setSection] = useState<string | null>('overview');
  const [searchQuery, setSearchQuery] = useState('');
  const knowledgePdfRef = useRef<KnowledgePdfModule | null>(null);
  const [knowledgePdfReady, setKnowledgePdfReady] = useState(false);

  useEffect(() => {
    let alive = true;
    void import('../utils/knowledgePdfExport')
      .then((m) => {
        if (!alive) return;
        knowledgePdfRef.current = m;
        setKnowledgePdfReady(true);
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

  const needle = norm(searchQuery);
  const statusHits = useMemo(
    () => STATUS_ENTRIES.filter((e) => matchesStatus(e, needle)),
    [needle]
  );
  const glossaryHits = useMemo(
    () => GLOSSARY_ENTRIES.filter((e) => matchesGlossary(e, needle)),
    [needle]
  );
  const processHits = useMemo(
    () => KNOWLEDGE_PROCESS_BLOCKS.filter((b) => matchesProcessBlock(b, needle)),
    [needle]
  );

  return (
    <Stack gap="md" maw={900} mx="auto">
      <Group justify="space-between" align="flex-end" wrap="wrap" gap="sm">
        <Title order={2} fw={700}>
          База знаний
        </Title>
        <TextInput
          placeholder="Поиск по статусам, словарю и процессам…"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.currentTarget.value)}
          leftSection={<IconSearch size={16} stroke={1.5} />}
          rightSection={
            searchQuery ? (
              <CloseButton aria-label="Очистить" onClick={() => setSearchQuery('')} iconSize={16} />
            ) : null
          }
          style={{ flex: 1, minWidth: 260, maxWidth: 420 }}
        />
      </Group>

      {needle ? (
        <Text size="sm" c="dimmed">
          Найдено:{' '}
          <Text span fw={600} c="indigo" style={{ cursor: 'pointer' }} onClick={() => setSection('statuses')}>
            статусов — {statusHits.length}
          </Text>
          {', '}
          <Text span fw={600} c="teal" style={{ cursor: 'pointer' }} onClick={() => setSection('glossary')}>
            терминов — {glossaryHits.length}
          </Text>
          {', '}
          <Text span fw={600} c="violet" style={{ cursor: 'pointer' }} onClick={() => setSection('processes')}>
            блоков процессов — {processHits.length}
          </Text>
          . Ниже показано содержимое активной вкладки с учётом запроса.
        </Text>
      ) : null}

      <Tabs value={section} onChange={setSection} variant="outline" radius="md" keepMounted={false}>
        <Tabs.List grow>
          <Tabs.Tab value="overview" leftSection={<IconLayoutDashboard size={16} />}>
            Обзор
          </Tabs.Tab>
          <Tabs.Tab value="statuses" leftSection={<IconListDetails size={16} />}>
            Описание статусов
          </Tabs.Tab>
          <Tabs.Tab value="glossary" leftSection={<IconVocabulary size={16} />}>
            Словарь
          </Tabs.Tab>
          <Tabs.Tab value="processes" leftSection={<IconGitBranch size={16} stroke={1.5} />}>
            Бизнес-процессы
          </Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="overview" pt="md">
          <KnowledgeOverviewSection
            onOpenStatuses={() => setSection('statuses')}
            onOpenGlossary={() => setSection('glossary')}
            onOpenProcesses={() => setSection('processes')}
          />
        </Tabs.Panel>

        <Tabs.Panel value="statuses" pt="md">
          <KnowledgeStatusesSection
            searchQuery={searchQuery}
            knowledgePdfRef={knowledgePdfRef}
            pdfReady={knowledgePdfReady}
          />
        </Tabs.Panel>

        <Tabs.Panel value="glossary" pt="md">
          <KnowledgeGlossarySection
            searchQuery={searchQuery}
            knowledgePdfRef={knowledgePdfRef}
            pdfReady={knowledgePdfReady}
          />
        </Tabs.Panel>

        <Tabs.Panel value="processes" pt="md">
          <KnowledgeProcessesSection searchQuery={searchQuery} />
        </Tabs.Panel>
      </Tabs>
    </Stack>
  );
}
