import { useState } from 'react';
import {
  Stack, Box, Title, Text, Paper, Group, Select, Table, ScrollArea,
  Pagination, Badge, Anchor, Center, Loader, Alert, ActionIcon,
} from '@mantine/core';
import { IconAlertCircle, IconUserCircle } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { getBoTasks } from '../api/backoffice';
import { PageSizeSelect } from '../components/common/PageSizeSelect';
import { ResizableTh } from '../components/common/ResizableTh';
import { STATUS_LABELS, STATUS_COLORS } from '../types';
import { fmtDateTime } from '../utils/format';
import { UserProfileModal } from '../components/common/UserProfileModal';
import { useDefaultPageSize } from '../hooks/useDefaultPageSize';

const TASK_STATUS_LABELS: Record<number, string> = {
  0: 'Открыто',
  1: 'Возвращено оператору',
  2: 'Группа удалена',
  3: 'Завершено',
};

const TYPE_OPTIONS = [
  { value: '', label: 'Все задания' },
  { value: '120', label: 'Нет продукта (120)' },
  { value: '320', label: 'Нет прав (320)' },
];

export function BackOfficeTasksPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useDefaultPageSize();
  const [taskType, setTaskType] = useState<string>('');
  const [selectedAccountId, setSelectedAccountId] = useState<number | null>(null);

  const handlePageSize = (v: number) => { setPageSize(v); setPage(1); };
  const handleType = (v: string | null) => { setTaskType(v ?? ''); setPage(1); };

  const { data, isLoading, error } = useQuery({
    queryKey: ['bo-tasks', page, pageSize, taskType],
    queryFn: () =>
      getBoTasks({
        pageNumber: page,
        pageSize,
        taskType: taskType ? Number(taskType) : null,
      }),
  });

  return (
    <Stack gap="xl">
      <Box>
        <Title order={3} fw={600}>Бэк-офис — задания</Title>
        <Text c="dimmed" size="sm">Активные задания, требующие обработки сотрудником бэк-офиса</Text>
      </Box>

      <Paper withBorder radius="md" p="sm">
        <Group gap="sm" align="flex-end">
          <Select
            label="Тип задания"
            size="xs"
            w={200}
            data={TYPE_OPTIONS}
            value={taskType}
            onChange={handleType}
          />
        </Group>
      </Paper>

      <Paper withBorder radius="md">
        {isLoading ? (
          <Center py="xl"><Loader size="sm" /></Center>
        ) : error ? (
          <Alert icon={<IconAlertCircle size={16} />} color="red" m="md">
            {error instanceof Error ? error.message : 'Ошибка загрузки'}
          </Alert>
        ) : !data?.items.length ? (
          <Center py="xl"><Text c="dimmed">Нет активных заданий</Text></Center>
        ) : (
          <ScrollArea>
            <Table striped highlightOnHover style={{ minWidth: 900 }}>
              <Table.Thead>
                <Table.Tr>
                  <ResizableTh>ID</ResizableTh>
                  <ResizableTh>Группа</ResizableTh>
                  <ResizableTh>Статус группы</ResizableTh>
                  <ResizableTh>Суспенсов</ResizableTh>
                  <ResizableTh>Артист / Название</ResizableTh>
                  <ResizableTh>Описание проблемы</ResizableTh>
                  <ResizableTh>Создал</ResizableTh>
                  <ResizableTh>Создано</ResizableTh>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {data.items.map((task) => (
                  <Table.Tr
                    key={task.id}
                    style={{ cursor: 'pointer' }}
                    onClick={() => navigate(`/backoffice/tasks/${task.id}`)}
                  >
                    <Table.Td>
                      <Anchor size="sm" onClick={(e) => { e.stopPropagation(); navigate(`/backoffice/tasks/${task.id}`); }}>
                        #{task.id}
                      </Anchor>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">#{task.groupId}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Badge color={STATUS_COLORS[task.groupStatus] ?? 'gray'} size="sm" variant="light">
                        {STATUS_LABELS[task.groupStatus] ?? task.groupStatus}
                      </Badge>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{task.suspenseCount}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Stack gap={0}>
                        {task.artist && <Text size="sm" fw={500}>{task.artist}</Text>}
                        {task.title && <Text size="xs" c="dimmed">{task.title}</Text>}
                        {!task.artist && !task.title && <Text size="xs" c="dimmed">—</Text>}
                      </Stack>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" lineClamp={2} maw={300}>{task.problemDescription}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Group gap={6}>
                        <ActionIcon
                          variant="subtle"
                          color="gray"
                          size="sm"
                          onClick={(e) => {
                            e.stopPropagation();
                            setSelectedAccountId(task.createdByAccountId);
                          }}
                          aria-label="Открыть профиль пользователя"
                        >
                          <IconUserCircle size={14} />
                        </ActionIcon>
                        <Text
                          size="sm"
                          style={{ cursor: 'pointer' }}
                          onClick={(e) => {
                            e.stopPropagation();
                            setSelectedAccountId(task.createdByAccountId);
                          }}
                        >
                          {task.createdByName ?? task.createdByLogin}
                        </Text>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">{fmtDateTime(task.createTime)}</Text>
                    </Table.Td>
                  </Table.Tr>
                ))}
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
      </Paper>
      <UserProfileModal
        opened={!!selectedAccountId}
        onClose={() => setSelectedAccountId(null)}
        accountId={selectedAccountId}
      />
    </Stack>
  );
}
