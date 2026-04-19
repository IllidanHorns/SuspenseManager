import { useState, useEffect } from 'react';
import {
  Drawer, Stack, Text, Group, Badge, Pagination, Skeleton, Alert, Paper, ScrollArea, ThemeIcon, Box,
} from '@mantine/core';
import { IconHistory, IconFolder, IconList, IconArrowRight } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { getAccountActivity, type AccountActivityItem } from '../../api/accountActivity';
import { fmtDateTime } from '../../utils/format';

interface AdminAccountActivityDrawerProps {
  opened: boolean;
  onClose: () => void;
  accountId: number | null;
  loginLabel: string;
}

const PAGE_SIZE = 12;

function ActivityRow({ item }: { item: AccountActivityItem }) {
  const from =
    item.statusFromName != null
      ? `${item.statusFromName} (${item.statusFrom})`
      : item.statusFrom != null
        ? String(item.statusFrom)
        : '—';
  const to = item.statusToName != null ? `${item.statusToName} (${item.statusTo})` : String(item.statusTo);

  return (
    <Paper withBorder radius="md" p="sm" bg="var(--mantine-color-body)">
      <Group justify="space-between" align="flex-start" wrap="nowrap" gap="sm">
        <ThemeIcon
          size={36}
          radius="md"
          variant="light"
          color={item.kind === 'group' ? 'indigo' : 'cyan'}
        >
          {item.kind === 'group' ? <IconFolder size={18} /> : <IconList size={18} />}
        </ThemeIcon>
        <Box style={{ flex: 1, minWidth: 0 }}>
          <Group gap={6} mb={4}>
            <Badge size="xs" variant="outline" color={item.kind === 'group' ? 'indigo' : 'cyan'}>
              {item.kind === 'group' ? 'Группа' : 'Строка'}
            </Badge>
            <Text size="xs" c="dimmed" ff="monospace">
              {item.kind === 'group' ? `Группа #${item.entityId}` : `Строка #${item.entityId}`}
              {item.kind === 'line' && item.groupId != null ? ` · гр. ${item.groupId}` : ''}
            </Text>
          </Group>
          <Group gap={6} align="center" wrap="wrap">
            <Text size="sm" lineClamp={2}>
              {from}
            </Text>
            <IconArrowRight size={14} style={{ opacity: 0.5 }} />
            <Text size="sm" fw={600} lineClamp={2}>
              {to}
            </Text>
          </Group>
          <Text size="xs" c="dimmed" mt={4}>
            {fmtDateTime(item.operationTime)}
          </Text>
        </Box>
      </Group>
    </Paper>
  );
}

export function AdminAccountActivityDrawer({
  opened,
  onClose,
  accountId,
  loginLabel,
}: AdminAccountActivityDrawerProps) {
  const [page, setPage] = useState(1);

  const { data, isLoading, error, isFetching } = useQuery({
    queryKey: ['admin-account-activity', accountId, page],
    queryFn: () =>
      getAccountActivity(accountId!, { pageNumber: page, pageSize: PAGE_SIZE }),
    enabled: opened && accountId != null && accountId > 0,
  });

  const items = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  useEffect(() => {
    if (opened) setPage(1);
  }, [opened, accountId]);

  return (
    <Drawer
      opened={opened}
      onClose={() => {
        setPage(1);
        onClose();
      }}
      position="right"
      size="lg"
      title={
        <Group gap="xs">
          <IconHistory size={22} />
          <div>
            <Text fw={600}>Активность</Text>
            <Text size="xs" c="dimmed">
              {loginLabel}
            </Text>
          </div>
        </Group>
      }
      padding="lg"
      scrollAreaComponent={ScrollArea.Autosize}
    >
      <Text size="sm" c="dimmed" mb="md">
        Переходы статусов по группам и строкам суспенсов, где в логе указан этот аккаунт (из аудита).
      </Text>

      {error && (
        <Alert color="red" mb="md" title="Не удалось загрузить">
          {(error as Error).message}
        </Alert>
      )}

      {isLoading ? (
        <Stack gap="sm">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} height={72} radius="md" />
          ))}
        </Stack>
      ) : (
        <>
          <Stack gap="sm" mb="md" opacity={isFetching ? 0.65 : 1}>
            {items.length === 0 ? (
              <Text size="sm" c="dimmed" ta="center" py="xl">
                Записей пока нет — действия с группами/строками появятся здесь после работы в системе.
              </Text>
            ) : (
              items.map((item) => <ActivityRow key={`${item.kind}-${item.logId}`} item={item} />)
            )}
          </Stack>

          {totalPages > 1 && (
            <Group justify="center">
              <Pagination total={totalPages} value={page} onChange={setPage} size="sm" />
            </Group>
          )}
        </>
      )}
    </Drawer>
  );
}
