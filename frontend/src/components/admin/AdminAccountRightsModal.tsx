import { useMemo, useState, useEffect } from 'react';
import {
  Modal, Stack, Text, Button, Group, ScrollArea, TextInput, Select, Checkbox, Paper, Divider, Badge,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { IconShieldCheck } from '@tabler/icons-react';
import {
  getRightsCatalog,
  getRolePresets,
  type RightCatalogItem,
  type RolePreset,
} from '../../api/rights';
import { getAccountRights, replaceAccountRights } from '../../api/accountsAdmin';

interface AdminAccountRightsModalProps {
  opened: boolean;
  onClose: () => void;
  accountId: number | null;
  loginLabel: string;
}

export function AdminAccountRightsModal({ opened, onClose, accountId, loginLabel }: AdminAccountRightsModalProps) {
  const qc = useQueryClient();
  const [filter, setFilter] = useState('');
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [presetId, setPresetId] = useState<string | null>(null);

  const { data: catalog = [], isLoading: catLoading } = useQuery({
    queryKey: ['rights-catalog'],
    queryFn: getRightsCatalog,
    enabled: opened,
  });

  const { data: presets = [] } = useQuery({
    queryKey: ['rights-presets'],
    queryFn: getRolePresets,
    enabled: opened,
  });

  const { data: currentRights, isLoading: curLoading } = useQuery({
    queryKey: ['account-rights', accountId],
    queryFn: () => getAccountRights(accountId!),
    enabled: opened && accountId != null && accountId > 0,
  });

  useEffect(() => {
    if (!opened || !currentRights) return;
    const ids = new Set(currentRights.map((r) => r.id));
    setSelected(ids);
    setFilter('');
    const matched = presets.find(
      (p) => p.rightIds.length === ids.size && p.rightIds.every((id) => ids.has(id))
    );
    setPresetId(matched?.id ?? null);
  }, [opened, currentRights, accountId, presets]);

  const byModule = useMemo(() => {
    const q = filter.trim().toLowerCase();
    const filtered = !q
      ? catalog
      : catalog.filter(
          (r) =>
            r.code.toLowerCase().includes(q) ||
            r.name.toLowerCase().includes(q) ||
            (r.module?.toLowerCase().includes(q) ?? false)
        );
    const map = new Map<string, RightCatalogItem[]>();
    for (const r of filtered) {
      const m = r.module || 'Прочее';
      if (!map.has(m)) map.set(m, []);
      map.get(m)!.push(r);
    }
    return [...map.entries()].sort((a, b) => a[0].localeCompare(b[0], 'ru'));
  }, [catalog, filter]);

  const toggle = (id: number) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
    setPresetId(null);
  };

  const applyPreset = (id: string | null) => {
    setPresetId(id);
    if (!id) return;
    const p = presets.find((x) => x.id === id);
    if (p) setSelected(new Set(p.rightIds));
  };

  const saveMutation = useMutation({
    mutationFn: () => replaceAccountRights(accountId!, [...selected]),
    onSuccess: () => {
      notifications.show({ title: 'Сохранено', message: 'Права аккаунта обновлены', color: 'green' });
      qc.invalidateQueries({ queryKey: ['account-rights', accountId] });
      qc.invalidateQueries({ queryKey: ['admin-accounts'] });
      onClose();
    },
    onError: (e: Error) => {
      notifications.show({ title: 'Ошибка', message: e.message, color: 'red' });
    },
  });

  const presetOptions = useMemo(
    () =>
      presets.map((p: RolePreset) => ({
        value: p.id,
        label: p.name,
        description: p.description,
      })),
    [presets]
  );

  const loading = catLoading || curLoading;

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={
        <Group gap="xs">
          <IconShieldCheck size={22} />
          <Text fw={600}>Права: {loginLabel}</Text>
        </Group>
      }
      size="xl"
      centered
      scrollAreaComponent={ScrollArea.Autosize}
    >
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          Отметьте права или выберите готовую роль. Сохранение полностью заменяет предыдущий набор.
        </Text>

        <Select
          label="Готовая роль"
          placeholder="Выберите пресет"
          data={presetOptions}
          value={presetId}
          onChange={applyPreset}
          clearable
          searchable
        />

        <TextInput
          label="Фильтр"
          placeholder="Код, название или модуль"
          value={filter}
          onChange={(e) => setFilter(e.currentTarget.value)}
        />

        {loading ? (
          <Text size="sm">Загрузка…</Text>
        ) : (
          <ScrollArea h={380} type="auto" offsetScrollbars>
            <Stack gap="md">
              {byModule.map(([module, items]) => (
                <Paper key={module} withBorder p="sm" radius="md">
                  <Group justify="space-between" mb="xs">
                    <Text fw={600} size="sm">
                      {module}
                    </Text>
                    <Badge size="sm" variant="light">
                      {items.filter((i) => selected.has(i.id)).length}/{items.length}
                    </Badge>
                  </Group>
                  <Stack gap="xs">
                    {items.map((r) => (
                      <Checkbox
                        key={r.id}
                        label={
                          <div>
                            <Text size="sm" fw={500}>
                              {r.name}
                            </Text>
                            <Text size="xs" c="dimmed" ff="monospace">
                              {r.code}
                            </Text>
                          </div>
                        }
                        checked={selected.has(r.id)}
                        onChange={() => toggle(r.id)}
                      />
                    ))}
                  </Stack>
                </Paper>
              ))}
            </Stack>
          </ScrollArea>
        )}

        <Divider />

        <Group justify="space-between">
          <Badge size="lg" variant="outline" color="indigo">
            Выбрано прав: {selected.size}
          </Badge>
          <Group>
            <Button variant="default" onClick={onClose}>
              Отмена
            </Button>
            <Button
              color="indigo"
              loading={saveMutation.isPending}
              disabled={!accountId}
              onClick={() => saveMutation.mutate()}
            >
              Сохранить
            </Button>
          </Group>
        </Group>
      </Stack>
    </Modal>
  );
}
