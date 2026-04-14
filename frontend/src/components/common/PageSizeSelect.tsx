import { Select, Text, Group } from '@mantine/core';

const PAGE_SIZE_OPTIONS = ['2', '5', '10', '15', '20', '50', '100'];

interface PageSizeSelectProps {
  value: number;
  onChange: (value: number) => void;
}

export function PageSizeSelect({ value, onChange }: PageSizeSelectProps) {
  return (
    <Group gap={6} align="center">
      <Text size="xs" c="dimmed">Строк:</Text>
      <Select
        size="xs"
        value={String(value)}
        onChange={(v) => v && onChange(Number(v))}
        data={PAGE_SIZE_OPTIONS}
        style={{ width: 70 }}
        allowDeselect={false}
      />
    </Group>
  );
}
