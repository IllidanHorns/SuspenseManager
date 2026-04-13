import { Badge } from '@mantine/core';
import { STATUS_LABELS, STATUS_COLORS } from '../../types';

export function StatusBadge({ status }: { status: number }) {
  return (
    <Badge color={STATUS_COLORS[status] ?? 'gray'} variant="light" size="sm">
      {STATUS_LABELS[status] ?? `Статус ${status}`}
    </Badge>
  );
}
