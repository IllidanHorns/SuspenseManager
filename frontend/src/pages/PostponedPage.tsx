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
import { getPostponedGroups } from '../api/groups';
import { returnFromPostponed, ungroupGroup } from '../api/processing';
import { fmtDateTime } from '../utils/format';
import { StatusBadge } from '../components/common/StatusBadge';
import { PageSizeSelect } from '../components/common/PageSizeSelect';
import { notifications } from '@mantine/notifications';

export function PostponedPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [returning, setReturning] = useState<number | null>(null);
  const [ungroupTarget, setUngroupTarget] = useState<number | null>(null);
  const [ungroupLoading, setUngroupLoading] = useState(false);
  const [confirmOpened, { open: openConfirm, close: closeConfirm }] = useDisclosure(false);
  const navigate = useNavigate();
  const qc = useQueryClient();

  const [pendingId, setPendingId] = useState('');
  const [applied, setApplied] = useState<Record<string, string>>({});
  const hasActive = Object.keys(applied).length > 0;

  const applyFilters = () => {
    const f: Record<string, string> = {};
    if (pendingId.trim()) f['Id'] = pendingId.trim();
    setApplied(f);
    setPage(1);
  };

  const resetFilters = () => {
    setPendingId('');
    setApplied({});
    setPage(1);
  };

  const handlePageSizeChange = (v: number) => { setPageSize(v); setPage(1); };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['postponed', page, pageSize, applied],
    queryFn: () => getPostponedGroups({ pageNumber: page, pageSize, Filters: applied }),
  });

  const handleReturn = async (groupId: number) => {
    setReturning(groupId);
    try {
      await returnFromPostponed(groupId);
      notifications.show({
        title: 'Группа возвращена',
        message: `Группа #${groupId} возвращена в обработку`,
        color: 'green',
      });
      await qc.invalidateQueries({ queryKey: ['postponed'] });
    } catch (e: unknown) {
      notifications.show({
        title: 'Ошибка',
        message: e instanceof Error ? e.message : 'Ошибка',
        color: 'red',
      });
    } finally {
      setReturning(null);
    }
  };

  const openUngroupConfirm = (groupId: number) => {
    setUngroupTarget(groupId);
    openConfirm();
  };

  const handleUngroup = async () => {
    if (!ungroupTarget) return;
    setUngroupLoading(true);
    try {
      await ungroupGroup(ungroupTarget);
      notifications.show({
        title: 'Группа расформирована',
        message: `Группа #${ungroupTarget} расформирована, строки возвращены в очередь`,
        color: 'gray',
      });
      closeConfirm();
      await qc.invalidateQueries({ queryKey: ['postponed'] });
    } catch (e: unknown) {
      notifications.show({
        title: 'Ошибка',
        message: e instanceof Error ? e.message : 'Ошибка',
        color: 'red',
      });
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

      <Group justify="flex-end">
        <Tooltip label="Обновить">
          <ActionIcon variant="light" color="gray" onClick={() => refetch()}>
            <IconRefresh size={16} />
          </ActionIcon>
        </Tooltip>
      </Group>

      {/* Filter bar */}
      <Paper withBorder radius="md" p="sm">
        <Group gap="sm" align="flex-end">
          <TextInput
            size="xs"
            label="ID группы"
            placeholder="Например: 42"
            value={pendingId}
            onChange={(e) => setPendingId(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && applyFilters()}
            style={{ width: 160 }}
          />
          <Group gap="xs">
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
      </Paper>

      {isLoading ? (
        <Center py="xl"><Loader color="indigo" /></Center>
      ) : error ? (
        <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">
          {error.message}
        </Alert>
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
                  <Table.Th>ID</Table.Th>
                  <Table.Th>Статус</Table.Th>
                  <Table.Th>Строк</Table.Th>
                  <Table.Th>Отложена</Table.Th>
                  <Table.Th style={{ textAlign: 'center' }}>Действия</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {data.items.map((g) => (
                  <Table.Tr key={g.id}>
                    <Table.Td>
                      <Text size="sm" fw={600} c="indigo">#{g.id}</Text>
                    </Table.Td>
                    <Table.Td><StatusBadge status={g.businessStatus} /></Table.Td>
                    <Table.Td>
                      <Badge variant="light" color="blue" size="sm">
                        {g.suspenseCount ?? '—'}
                      </Badge>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">{fmtDateTime(g.changeTime ?? g.createTime)}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Group justify="center" gap="xs">
                        <Tooltip label="Открыть">
                          <ActionIcon
                            variant="light"
                            color="indigo"
                            size="sm"
                            onClick={() => navigate(`/groups/${g.id}`)}
                          >
                            <IconEye size={14} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Вернуть в обработку">
                          <ActionIcon
                            variant="light"
                            color="green"
                            size="sm"
                            loading={returning === g.id}
                            onClick={() => handleReturn(g.id)}
                          >
                            <IconArrowBack size={14} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Расформировать группу">
                          <ActionIcon
                            variant="light"
                            color="red"
                            size="sm"
                            onClick={() => openUngroupConfirm(g.id)}
                          >
                            <IconUnlink size={14} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
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

      <Modal
        opened={confirmOpened}
        onClose={closeConfirm}
        title="Расформировать группу"
        centered
        radius="md"
      >
        <Stack gap="md">
          <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">
            Группа #{ungroupTarget} будет архивирована. Все суспенсы вернутся в исходный статус.
            Это действие нельзя отменить.
          </Alert>
          <Group justify="flex-end">
            <Button variant="subtle" color="gray" onClick={closeConfirm}>
              Отмена
            </Button>
            <Button color="red" loading={ungroupLoading} onClick={handleUngroup}>
              Расформировать
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}
