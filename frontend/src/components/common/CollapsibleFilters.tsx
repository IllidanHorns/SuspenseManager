import { Badge, Box, Button, Collapse, Group } from '@mantine/core';
import { IconChevronDown, IconChevronUp, IconFilter } from '@tabler/icons-react';
import { useState, useEffect, useRef, type ReactNode } from 'react';
import { useMeSettingsQuery } from '../../hooks/useMeSettings';

interface CollapsibleFiltersProps {
  /** Число применённых фильтров (для бейджа) */
  activeCount: number;
  /** Подпись на кнопке */
  label?: string;
  /** Явное начальное состояние — переопределяет глобальную настройку */
  defaultOpened?: boolean;
  children: ReactNode;
}

export function CollapsibleFilters({
  activeCount,
  label = 'Фильтры',
  defaultOpened,
  children,
}: CollapsibleFiltersProps) {
  const { data: meSettings } = useMeSettingsQuery();
  const [opened, setOpened] = useState(defaultOpened ?? false);
  const hydrated = useRef(false);

  useEffect(() => {
    if (hydrated.current || !meSettings?.preferences) return;
    hydrated.current = true;
    // Явный defaultOpened (true/false) имеет приоритет над глобальной настройкой
    if (defaultOpened === undefined) {
      setOpened(meSettings.preferences.filtersExpandedByDefault);
    }
  }, [meSettings, defaultOpened]);

  return (
    <Box>
      <Group gap="xs" mb={opened ? 'xs' : 0}>
        <Button
          variant="light"
          color="gray"
          size="sm"
          leftSection={<IconFilter size={16} />}
          rightSection={opened ? <IconChevronUp size={16} /> : <IconChevronDown size={16} />}
          onClick={() => setOpened((o) => !o)}
        >
          <Group gap={8} wrap="nowrap">
            {label}
            {activeCount > 0 && (
              <Badge size="xs" variant="filled" color="indigo">
                {activeCount > 99 ? '99+' : activeCount}
              </Badge>
            )}
          </Group>
        </Button>
      </Group>
      <Collapse in={opened}>{children}</Collapse>
    </Box>
  );
}
